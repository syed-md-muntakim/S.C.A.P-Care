using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Repositories;
using SmartClinic.Web.Services.Foundation;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Services;

public interface IAppointmentService
{
    const int MaxAdvanceDays = 60;

    Task<List<SelectListItem>> DoctorOptionsAsync();
    Task<List<SelectListItem>> PatientOptionsAsync();
    Task<BookingDay> GetDayAsync(string doctorId, DateOnly date);
    Task<ServiceResult> BookAsync(BookingRequest request);
    Task<AppointmentIndexViewModel> GetListAsync(AppointmentListQuery query, string userId, string role);
    Task<int> CountUpcomingForPatientAsync(string patientId);
    Task<int> CountTodayForDoctorAsync(string doctorId);
    Task<int> CountTodayAsync();
}

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appts;
    private readonly IScheduleService _schedule;
    private readonly UserManager<ApplicationUser> _users;

    public AppointmentService(IAppointmentRepository appts, IScheduleService schedule, UserManager<ApplicationUser> users)
    {
        _appts = appts; _schedule = schedule; _users = users;
    }

    // ------------------------------------------------------------------ dropdowns
    public async Task<List<SelectListItem>> DoctorOptionsAsync() =>
        (await _users.GetUsersInRoleAsync(Roles.Doctor))
            .Where(d => d.IsApproved && d.IsActive).OrderBy(d => d.FullName)
            .Select(d => new SelectListItem($"{NameFormat.Doctor(d.FullName)} - {d.Specialty ?? "General"}", d.Id)).ToList();

    public async Task<List<SelectListItem>> PatientOptionsAsync() =>
        (await _users.GetUsersInRoleAsync(Roles.Patient))
            .Where(p => p.IsActive).OrderBy(p => p.FullName)
            .Select(p => new SelectListItem($"{p.FullName} ({p.UserName})", p.Id)).ToList();

    // ------------------------------------------------------------------ slots for one doctor/day
    public async Task<BookingDay> GetDayAsync(string doctorId, DateOnly date)
    {
        var today = ClinicTime.Today;
        var empty = Array.Empty<BookingSlot>();
        if (date < today) return new BookingDay(date.Iso(), true, false, null, empty);
        if (!await IsBookableDoctorAsync(doctorId)) return new BookingDay(date.Iso(), false, false, null, empty);

        var day = await _schedule.GetDayScheduleAsync(doctorId, date);
        if (day.IsOnLeave) return new BookingDay(date.Iso(), false, true, day.LeaveReason, empty);

        var booked = await _appts.BookedStartsAsync(doctorId, date);
        var now = ClinicTime.TimeNow;
        var slots = day.Slots
            .Where(s => date > today || s.Start > now)
            .Select(s => new BookingSlot(s.Start.Hm(), s.End.Hm(), booked.Contains(s.Start)))
            .ToList();
        return new BookingDay(date.Iso(), false, false, null, slots);
    }

    // ------------------------------------------------------------------ booking + conflict detection
    public async Task<ServiceResult> BookAsync(BookingRequest r)
    {
        var today = ClinicTime.Today;
        if (r.Date < today) return ServiceResult.Fail("You cannot book a date in the past.");
        if (r.Date > today.AddDays(IAppointmentService.MaxAdvanceDays))
            return ServiceResult.Fail($"Appointments can be booked up to {IAppointmentService.MaxAdvanceDays} days ahead.");

        if (!await IsBookableDoctorAsync(r.DoctorId)) return ServiceResult.Fail("The selected doctor is not available for booking.");

        var patient = await _users.FindByIdAsync(r.PatientId);
        if (patient is null || !patient.IsActive || !await _users.IsInRoleAsync(patient, Roles.Patient))
            return ServiceResult.Fail("Patient account not found or inactive.");

        // 1. The time must be a real slot of the doctor's schedule (shift, not on leave).
        var day = await _schedule.GetDayScheduleAsync(r.DoctorId, r.Date);
        if (day.IsOnLeave) return ServiceResult.Fail("The doctor is on leave on that date.");
        var slot = day.Slots.FirstOrDefault(s => s.Start == r.Start);
        if (slot is null) return ServiceResult.Fail("That time is not an available slot for this doctor.");
        if (r.Date == today && slot.Start <= ClinicTime.TimeNow) return ServiceResult.Fail("That time has already passed.");

        // 2. Doctor conflict: slot already taken.
        if (await _appts.DoctorSlotTakenAsync(r.DoctorId, r.Date, slot.Start))
            return ServiceResult.Fail("This slot is already booked. Please choose another time.");

        // 3. Patient conflicts: overlapping appointment, or a second visit to the same doctor that day.
        if (await _appts.PatientOverlapAsync(r.PatientId, r.Date, slot.Start, slot.End))
            return ServiceResult.Fail("The patient already has another appointment at this time.");
        if (await _appts.PatientHasBookingWithDoctorAsync(r.PatientId, r.DoctorId, r.Date))
            return ServiceResult.Fail("The patient already has an appointment with this doctor on that day.");

        _appts.Add(new Appointment
        {
            PatientId = r.PatientId, DoctorId = r.DoctorId, AppointmentDate = r.Date,
            StartTime = slot.Start, EndTime = slot.End, Status = AppointmentStatus.Booked,
            Reason = string.IsNullOrWhiteSpace(r.Reason) ? null : r.Reason.Trim(), BookedById = r.BookedById
        });

        try { await _appts.SaveAsync(); }
        catch (DbUpdateException)
        {   // 4. Race condition: another request took the slot after our checks (unique index fired).
            return ServiceResult.Fail("Someone just booked this slot. Please pick another time.");
        }
        return ServiceResult.Ok();
    }

    // ------------------------------------------------------------------ lists
    public async Task<AppointmentIndexViewModel> GetListAsync(AppointmentListQuery q, string userId, string role)
    {
        var today = ClinicTime.Today;
        var scope = q.Scope is "today" or "past" or "all" ? q.Scope : "upcoming";
        q.Scope = scope;

        var f = new AppointmentFilter { Status = q.Status };
        if (role == Roles.Patient) f.PatientId = userId;
        else if (role == Roles.Doctor) f.DoctorId = userId;
        else if (!string.IsNullOrEmpty(q.DoctorId)) f.DoctorId = q.DoctorId;

        switch (scope)
        {
            case "today": f.From = today; f.To = today; break;
            case "past": f.To = today.AddDays(-1); f.Descending = true; break;
            case "all": f.Descending = true; break;
            default: f.From = today; break;
        }

        var list = await _appts.QueryAsync(f);
        var items = list.Select(a => new AppointmentListItem
        {
            Id = a.Id, DoctorId = a.DoctorId,
            DoctorName = NameFormat.Doctor(a.Doctor?.FullName ?? "Doctor"), Specialty = a.Doctor?.Specialty,
            PatientName = a.Patient?.FullName ?? "Patient",
            Date = a.AppointmentDate, Start = a.StartTime, End = a.EndTime, Status = a.Status, Reason = a.Reason,
            TokenNumber = a.Token?.TokenNumber, TokenStatus = a.Token?.Status,
            CanGetToken = a.Status == AppointmentStatus.Booked && a.AppointmentDate == today && a.Token is null
        }).ToList();

        return new AppointmentIndexViewModel
        {
            Role = role, Query = q, Items = items,
            Doctors = role == Roles.Admin ? await DoctorOptionsAsync() : new()
        };
    }

    // ------------------------------------------------------------------ dashboard counts
    public Task<int> CountUpcomingForPatientAsync(string patientId) =>
        _appts.CountAsync(new AppointmentFilter { PatientId = patientId, From = ClinicTime.Today, Status = AppointmentStatus.Booked });

    public Task<int> CountTodayForDoctorAsync(string doctorId) =>
        _appts.CountAsync(new AppointmentFilter { DoctorId = doctorId, From = ClinicTime.Today, To = ClinicTime.Today, ExcludeCancelled = true });

    public Task<int> CountTodayAsync() =>
        _appts.CountAsync(new AppointmentFilter { From = ClinicTime.Today, To = ClinicTime.Today, ExcludeCancelled = true });

    private async Task<bool> IsBookableDoctorAsync(string doctorId)
    {
        var d = await _users.FindByIdAsync(doctorId);
        return d is not null && d.IsApproved && d.IsActive && await _users.IsInRoleAsync(d, Roles.Doctor);
    }
}
