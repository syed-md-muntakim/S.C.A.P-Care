using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;
using SmartClinic.Web.Services.Foundation;

namespace SmartClinic.Web.Controllers;


[Authorize(Roles = Roles.Doctor + "," + Roles.Admin)]
public class DoctorScheduleController : Controller
{
    private readonly IScheduleService _schedule;
    private readonly UserManager<ApplicationUser> _users;

    public DoctorScheduleController(IScheduleService schedule, UserManager<ApplicationUser> users)
    {
        _schedule = schedule; _users = users;
    }

    /// <summary>Whose schedule is this request about? null = not allowed / not found.</summary>
    private async Task<ApplicationUser?> ResolveDoctorAsync(string? doctorId)
    {
        var id = User.IsInRole(Roles.Admin) ? doctorId : User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) return null;
        var doc = await _users.FindByIdAsync(id);
        return doc is not null && await _users.IsInRoleAsync(doc, Roles.Doctor) ? doc : null;
    }

    private IActionResult BackToList(string doctorId) =>
        RedirectToAction(nameof(Index), new { doctorId });

    // ---------- shifts + leave overview ----------
    public async Task<IActionResult> Index(string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return User.IsInRole(Roles.Admin) ? RedirectToAction("Index", "AdminDoctors") : NotFound();

        return View(new ScheduleIndexViewModel
        {
            DoctorId = doc.Id, DoctorName = doc.FullName, Specialty = doc.Specialty,
            IsAdminView = User.IsInRole(Roles.Admin),
            Shifts = await _schedule.GetShiftsAsync(doc.Id),
            Leaves = await _schedule.GetLeavesAsync(doc.Id)
        });
    }

    // ---------- add shift (one or many weekdays at once) ----------
    [HttpGet]
    public async Task<IActionResult> AddShift(string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        return View("ShiftForm", new ShiftFormViewModel { DoctorId = doc.Id, DoctorName = doc.FullName, Days = new() { 0, 1, 2, 3, 4 } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddShift(ShiftFormViewModel model)
    {
        var doc = await ResolveDoctorAsync(model.DoctorId);
        if (doc is null) return NotFound();
        model.DoctorId = doc.Id; model.DoctorName = doc.FullName; model.Id = null;
        ModelState.Remove(nameof(model.DoctorId));
        if (!ModelState.IsValid) return View("ShiftForm", model);

        var result = await _schedule.AddShiftsAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not add the shift.");
            return View("ShiftForm", model);
        }
        TempData["Success"] = "Shift added.";
        return BackToList(doc.Id);
    }

    // ---------- edit shift ----------
    [HttpGet]
    public async Task<IActionResult> EditShift(int id, string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        var shift = await _schedule.GetShiftAsync(id, doc.Id);
        if (shift is null) return NotFound();

        return View("ShiftForm", new ShiftFormViewModel
        {
            Id = shift.Id, DoctorId = doc.Id, DoctorName = doc.FullName, Days = new() { (int)shift.DayOfWeek },
            StartTime = shift.StartTime, EndTime = shift.EndTime, SlotMinutes = shift.SlotMinutes,
            ValidFrom = shift.ValidFrom, ValidTo = shift.ValidTo
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditShift(ShiftFormViewModel model)
    {
        var doc = await ResolveDoctorAsync(model.DoctorId);
        if (doc is null) return NotFound();
        model.DoctorId = doc.Id; model.DoctorName = doc.FullName;
        ModelState.Remove(nameof(model.DoctorId));
        if (!ModelState.IsValid) return View("ShiftForm", model);

        var result = await _schedule.UpdateShiftAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update the shift.");
            return View("ShiftForm", model);
        }
        TempData["Success"] = "Shift updated.";
        return BackToList(doc.Id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleShift(int id, bool active, string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        var result = await _schedule.SetShiftActiveAsync(id, doc.Id, active);
        if (result.Success) TempData["Success"] = active ? "Shift resumed." : "Shift paused.";
        else TempData["Error"] = result.Error;
        return BackToList(doc.Id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShift(int id, string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        var result = await _schedule.DeleteShiftAsync(id, doc.Id);
        if (result.Success) TempData["Success"] = "Shift deleted.";
        else TempData["Error"] = result.Error;
        return BackToList(doc.Id);
    }

    // ---------- leave ----------
    [HttpGet]
    public async Task<IActionResult> AddLeave(string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        return View("LeaveForm", new LeaveFormViewModel { DoctorId = doc.Id, DoctorName = doc.FullName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLeave(LeaveFormViewModel model)
    {
        var doc = await ResolveDoctorAsync(model.DoctorId);
        if (doc is null) return NotFound();
        model.DoctorId = doc.Id; model.DoctorName = doc.FullName;
        ModelState.Remove(nameof(model.DoctorId));
        if (!ModelState.IsValid) return View("LeaveForm", model);

        var result = await _schedule.AddLeaveAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not add the leave.");
            return View("LeaveForm", model);
        }
        TempData["Success"] = "Leave added. No slots are offered on those days.";
        return BackToList(doc.Id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLeave(int id, string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        var result = await _schedule.DeleteLeaveAsync(id, doc.Id);
        if (result.Success) TempData["Success"] = "Leave removed.";
        else TempData["Error"] = result.Error;
        return BackToList(doc.Id);
    }

    // ---------- calendar (FullCalendar) ----------
    public async Task<IActionResult> Calendar(string? doctorId)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return User.IsInRole(Roles.Admin) ? RedirectToAction("Index", "AdminDoctors") : NotFound();

        return View(new ScheduleIndexViewModel
        {
            DoctorId = doc.Id, DoctorName = doc.FullName, Specialty = doc.Specialty,
            IsAdminView = User.IsInRole(Roles.Admin)
        });
    }

    /// <summary>JSON feed for FullCalendar. start/end arrive like 2026-11-01T00:00:00+06:00 - only the date part is used.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents(string? doctorId, string start, string end)
    {
        var doc = await ResolveDoctorAsync(doctorId);
        if (doc is null) return NotFound();
        if (!TryDate(start, out var from) || !TryDate(end, out var to)) return BadRequest();
        return Json(await _schedule.GetCalendarEventsAsync(doc.Id, from, to));
    }

    private static bool TryDate(string? s, out DateOnly d)
    {
        d = default;
        return s is { Length: >= 10 } &&
               DateOnly.TryParseExact(s[..10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.None, out d);
    }
}
