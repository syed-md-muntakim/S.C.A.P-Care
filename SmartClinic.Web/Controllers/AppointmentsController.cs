using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Services;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

/// <summary>Sprint 2 #2: appointment booking with conflict detection.</summary>
[Authorize]
public class AppointmentsController : Controller
{
    private readonly IAppointmentService _appts;
    public AppointmentsController(IAppointmentService appts) => _appts = appts;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string Role => User.IsInRole(Roles.Admin) ? Roles.Admin : User.IsInRole(Roles.Doctor) ? Roles.Doctor : Roles.Patient;

    // Patients see their own, doctors see theirs, admin sees everything.
    public async Task<IActionResult> Index([FromQuery] AppointmentListQuery query) =>
        View(await _appts.GetListAsync(query, UserId, Role));

    [Authorize(Roles = Roles.Patient + "," + Roles.Admin)]
    public async Task<IActionResult> Book(string? doctorId) =>
        View(await Prepare(new BookAppointmentViewModel { DoctorId = doctorId ?? string.Empty, Date = ClinicTime.Today }));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Patient + "," + Roles.Admin)]
    public async Task<IActionResult> Book(BookAppointmentViewModel model)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!isAdmin) { model.PatientId = UserId; }
        else if (string.IsNullOrEmpty(model.PatientId)) ModelState.AddModelError(nameof(model.PatientId), "Select a patient.");

        if (ModelState.IsValid)
        {
            var result = await _appts.BookAsync(new BookingRequest(
                model.PatientId!, model.DoctorId, model.Date!.Value, model.StartTime!.Value, model.Reason, UserId));
            if (result.Success)
            {
                TempData["Success"] = $"Appointment booked for {model.Date.Value.Pretty()} at {model.StartTime.Value.Hm()}.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(await Prepare(model));
    }

    // JSON used by the booking page: slots of one doctor on one date, booked ones flagged.
    [HttpGet, Authorize(Roles = Roles.Patient + "," + Roles.Admin)]
    public async Task<IActionResult> Slots(string doctorId, DateOnly date) => Json(await _appts.GetDayAsync(doctorId, date));

    private async Task<BookAppointmentViewModel> Prepare(BookAppointmentViewModel m)
    {
        m.IsAdmin = User.IsInRole(Roles.Admin);
        m.MinDate = ClinicTime.Today.Iso();
        m.MaxDate = ClinicTime.Today.AddDays(IAppointmentService.MaxAdvanceDays).Iso();
        m.Doctors = await _appts.DoctorOptionsAsync();
        if (m.IsAdmin) m.Patients = await _appts.PatientOptionsAsync();
        return m;
    }
}
