using SmartClinic.Web.Common;
using SmartClinic.Web.Data.Foundation;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Services.Foundation;


public interface IScheduleService
{
    Task<IReadOnlyList<DoctorShift>> GetShiftsAsync(string doctorId);
    Task<DoctorShift?> GetShiftAsync(int id, string doctorId);
    Task<IReadOnlyList<DoctorLeave>> GetLeavesAsync(string doctorId);

    Task<ServiceResult> AddShiftsAsync(ShiftFormViewModel model);
    Task<ServiceResult> UpdateShiftAsync(ShiftFormViewModel model);
    Task<ServiceResult> SetShiftActiveAsync(int id, string doctorId, bool active);
    Task<ServiceResult> DeleteShiftAsync(int id, string doctorId);

    Task<ServiceResult> AddLeaveAsync(LeaveFormViewModel model);
    Task<ServiceResult> DeleteLeaveAsync(int id, string doctorId);

    
    Task<DaySchedule> GetDayScheduleAsync(string doctorId, DateOnly date);
  
    Task<bool> IsValidSlotAsync(string doctorId, DateOnly date, TimeOnly start);
  
    Task<DateOnly?> NextAvailableDateAsync(string doctorId, DateOnly from, int horizonDays = 60);

    Task<IReadOnlyList<ScheduleEvent>> GetCalendarEventsAsync(string doctorId, DateOnly from, DateOnly to);
}

public class ScheduleService : IScheduleService
{
    private const string ShiftColor = "#5b21b6";
    private const string LeaveColor = "#dc2626";
    private const string PausedColor = "#9ca3af";

    private readonly IDoctorShiftRepository _shifts;
    private readonly IDoctorLeaveRepository _leaves;

    public ScheduleService(IDoctorShiftRepository shifts, IDoctorLeaveRepository leaves)
    {
        _shifts = shifts; _leaves = leaves;
    }

    // ------------------------------------------------------------------ queries
    public async Task<IReadOnlyList<DoctorShift>> GetShiftsAsync(string doctorId) => await _shifts.ListByDoctorAsync(doctorId);

    public async Task<DoctorShift?> GetShiftAsync(int id, string doctorId)
    {
        var s = await _shifts.GetAsync(id);
        return s is not null && s.DoctorId == doctorId ? s : null;
    }

    public async Task<IReadOnlyList<DoctorLeave>> GetLeavesAsync(string doctorId) => await _leaves.ListByDoctorAsync(doctorId);

    // ------------------------------------------------------------------ shifts
    public async Task<ServiceResult> AddShiftsAsync(ShiftFormViewModel m)
    {
        var invalid = ValidateShift(m);
        if (invalid is not null) return invalid;
        if (m.ValidTo is { } to && to < ClinicTime.Today)
            return ServiceResult.Fail("'Valid until' cannot be in the past.");

        var existing = await _shifts.ListByDoctorAsync(m.DoctorId);
        var toAdd = new List<DoctorShift>();
        foreach (var d in m.Days.Distinct())
        {
            var day = (DayOfWeek)d;
            var clash = existing.FirstOrDefault(s => Overlaps(s, day, m, null));
            if (clash is not null)
                return ServiceResult.Fail($"{day} {m.StartTime.Hm()}-{m.EndTime.Hm()} overlaps an existing shift ({clash.StartTime.Hm()}-{clash.EndTime.Hm()}).");

            toAdd.Add(new DoctorShift
            {
                DoctorId = m.DoctorId, DayOfWeek = day, StartTime = m.StartTime, EndTime = m.EndTime,
                SlotMinutes = m.SlotMinutes, ValidFrom = m.ValidFrom, ValidTo = m.ValidTo, IsActive = true
            });
        }
        _shifts.AddRange(toAdd);
        await _shifts.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateShiftAsync(ShiftFormViewModel m)
    {
        if (m.Id is null) return ServiceResult.Fail("Shift not found.");
        if (m.Days.Distinct().Count() != 1) return ServiceResult.Fail("Edit one day at a time: choose exactly one day.");
        var invalid = ValidateShift(m);
        if (invalid is not null) return invalid;

        var shift = await GetShiftAsync(m.Id.Value, m.DoctorId);
        if (shift is null) return ServiceResult.Fail("Shift not found.");

        var day = (DayOfWeek)m.Days[0];
        var existing = await _shifts.ListByDoctorAsync(m.DoctorId);
        var clash = existing.FirstOrDefault(s => Overlaps(s, day, m, shift.Id));
        if (clash is not null)
            return ServiceResult.Fail($"This change overlaps another shift ({clash.StartTime.Hm()}-{clash.EndTime.Hm()}).");

        shift.DayOfWeek = day; shift.StartTime = m.StartTime; shift.EndTime = m.EndTime;
        shift.SlotMinutes = m.SlotMinutes; shift.ValidFrom = m.ValidFrom; shift.ValidTo = m.ValidTo;
        await _shifts.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetShiftActiveAsync(int id, string doctorId, bool active)
    {
        var shift = await GetShiftAsync(id, doctorId);
        if (shift is null) return ServiceResult.Fail("Shift not found.");
        if (active)
        {
            var existing = await _shifts.ListByDoctorAsync(doctorId);
            var clash = existing.FirstOrDefault(s => s.Id != shift.Id && s.IsActive && SameWindow(s, shift));
            if (clash is not null) return ServiceResult.Fail("Cannot resume: it now overlaps another active shift.");
        }
        shift.IsActive = active;
        await _shifts.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteShiftAsync(int id, string doctorId)
    {
        var shift = await GetShiftAsync(id, doctorId);
        if (shift is null) return ServiceResult.Fail("Shift not found.");
        _shifts.SoftDelete(shift);
        await _shifts.SaveAsync();
        return ServiceResult.Ok();
    }

    // ------------------------------------------------------------------ leave
    public async Task<ServiceResult> AddLeaveAsync(LeaveFormViewModel m)
    {
        if (m.EndDate < m.StartDate) return ServiceResult.Fail("'To' date must be on or after 'From' date.");
        if (m.StartDate < ClinicTime.Today) return ServiceResult.Fail("Leave cannot start in the past.");
        if (m.EndDate.DayNumber - m.StartDate.DayNumber > 365) return ServiceResult.Fail("A single leave cannot be longer than one year.");

        var existing = await _leaves.ListByDoctorAsync(m.DoctorId);
        var clash = existing.FirstOrDefault(l => l.StartDate <= m.EndDate && m.StartDate <= l.EndDate);
        if (clash is not null)
            return ServiceResult.Fail($"Overlaps an existing leave ({clash.StartDate.Pretty()} - {clash.EndDate.Pretty()}).");

        _leaves.Add(new DoctorLeave
        {
            DoctorId = m.DoctorId, StartDate = m.StartDate, EndDate = m.EndDate,
            LeaveType = m.LeaveType, Reason = string.IsNullOrWhiteSpace(m.Reason) ? null : m.Reason.Trim()
        });
        await _leaves.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteLeaveAsync(int id, string doctorId)
    {
        var leave = await _leaves.GetAsync(id);
        if (leave is null || leave.DoctorId != doctorId) return ServiceResult.Fail("Leave not found.");
        _leaves.SoftDelete(leave);
        await _leaves.SaveAsync();
        return ServiceResult.Ok();
    }

    // ------------------------------------------------------------------ availability / slots
    public async Task<DaySchedule> GetDayScheduleAsync(string doctorId, DateOnly date) =>
        BuildDay(await _shifts.ListByDoctorAsync(doctorId), await _leaves.ListByDoctorAsync(doctorId), date);

    public async Task<bool> IsValidSlotAsync(string doctorId, DateOnly date, TimeOnly start)
    {
        var day = await GetDayScheduleAsync(doctorId, date);
        return day.Slots.Any(s => s.Start == start);
    }

    public async Task<DateOnly?> NextAvailableDateAsync(string doctorId, DateOnly from, int horizonDays = 60)
    {
        var shifts = await _shifts.ListByDoctorAsync(doctorId);
        var leaves = await _leaves.ListByDoctorAsync(doctorId);
        for (var i = 0; i <= horizonDays; i++)
        {
            var date = from.AddDays(i);
            var day = BuildDay(shifts, leaves, date);
            // today only counts if a slot is still in the future
            var hasSlot = date == ClinicTime.Today
                ? day.Slots.Any(s => s.Start > ClinicTime.TimeNow)
                : day.Slots.Count > 0;
            if (hasSlot) return date;
        }
        return null;
    }

    public async Task<IReadOnlyList<ScheduleEvent>> GetCalendarEventsAsync(string doctorId, DateOnly from, DateOnly to)
    {
        if (to < from) return Array.Empty<ScheduleEvent>();
        if (to.DayNumber - from.DayNumber > 62) to = from.AddDays(62);   // calendar never needs more than ~2 months

        var shifts = await _shifts.ListByDoctorAsync(doctorId);
        var leaves = await _leaves.ListByDoctorAsync(doctorId);
        var events = new List<ScheduleEvent>();

        foreach (var l in leaves.Where(l => l.StartDate <= to && from <= l.EndDate))
        {
            events.Add(new ScheduleEvent($"leave-{l.Id}", $"Leave: {l.LeaveType}", l.StartDate.Iso(),
                l.EndDate.AddDays(1).Iso(), true, LeaveColor));   // FullCalendar all-day end is exclusive
        }

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (leaves.Any(l => l.StartDate <= date && date <= l.EndDate)) continue;
            foreach (var s in shifts.Where(s => AppliesOn(s, date)))
            {
                events.Add(new ScheduleEvent($"shift-{s.Id}-{date.Iso()}",
                    $"{s.StartTime.Hm()}-{s.EndTime.Hm()} ({s.SlotMinutes} min)",
                    $"{date.Iso()}T{s.StartTime.Hm()}:00", $"{date.Iso()}T{s.EndTime.Hm()}:00",
                    false, s.IsActive ? ShiftColor : PausedColor));
            }
        }
        return events;
    }

    // ------------------------------------------------------------------ helpers
    private static DaySchedule BuildDay(IEnumerable<DoctorShift> shifts, IEnumerable<DoctorLeave> leaves, DateOnly date)
    {
        var leave = leaves.FirstOrDefault(l => l.StartDate <= date && date <= l.EndDate);
        if (leave is not null)
            return new DaySchedule(date, true, leave.Reason ?? leave.LeaveType.ToString(), Array.Empty<TimeSlot>());

        var slots = new List<TimeSlot>();
        foreach (var s in shifts.Where(s => s.IsActive && AppliesOn(s, date)))
        {
            var startMin = s.StartTime.Hour * 60 + s.StartTime.Minute;
            var endMin = s.EndTime.Hour * 60 + s.EndTime.Minute;
            for (var m = startMin; m + s.SlotMinutes <= endMin; m += s.SlotMinutes)
            {
                slots.Add(new TimeSlot(
                    TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(m)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(m + s.SlotMinutes))));
            }
        }
        return new DaySchedule(date, false, null, slots.OrderBy(x => x.Start).ToList());
    }

    private static bool AppliesOn(DoctorShift s, DateOnly date) =>
        s.DayOfWeek == date.DayOfWeek && s.ValidFrom <= date && (s.ValidTo is null || date <= s.ValidTo);

    /// <summary>Same weekday, overlapping clock times and overlapping validity period (only active shifts clash).</summary>
    private static bool Overlaps(DoctorShift s, DayOfWeek day, ShiftFormViewModel m, int? ignoreId)
    {
        if (!s.IsActive || s.Id == ignoreId || s.DayOfWeek != day) return false;
        var timeOverlap = s.StartTime < m.EndTime && m.StartTime < s.EndTime;
        var sTo = s.ValidTo ?? DateOnly.MaxValue;
        var mTo = m.ValidTo ?? DateOnly.MaxValue;
        var dateOverlap = s.ValidFrom <= mTo && m.ValidFrom <= sTo;
        return timeOverlap && dateOverlap;
    }

    private static bool SameWindow(DoctorShift a, DoctorShift b)
    {
        if (a.DayOfWeek != b.DayOfWeek) return false;
        var timeOverlap = a.StartTime < b.EndTime && b.StartTime < a.EndTime;
        var dateOverlap = a.ValidFrom <= (b.ValidTo ?? DateOnly.MaxValue) && b.ValidFrom <= (a.ValidTo ?? DateOnly.MaxValue);
        return timeOverlap && dateOverlap;
    }

    private static ServiceResult? ValidateShift(ShiftFormViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.DoctorId)) return ServiceResult.Fail("Doctor is required.");
        if (m.Days.Count == 0) return ServiceResult.Fail("Select at least one day of the week.");
        if (m.Days.Any(d => d < 0 || d > 6)) return ServiceResult.Fail("Invalid day of week.");
        if (m.EndTime <= m.StartTime) return ServiceResult.Fail("End time must be after start time.");
        if (m.SlotMinutes < 5 || m.SlotMinutes > 120) return ServiceResult.Fail("Slot length must be between 5 and 120 minutes.");
        if ((m.EndTime - m.StartTime).TotalMinutes < m.SlotMinutes) return ServiceResult.Fail("The shift is shorter than one slot.");
        if (m.ValidTo is { } to && to < m.ValidFrom) return ServiceResult.Fail("'Valid until' must be on or after 'Valid from'.");
        return null;
    }
}
