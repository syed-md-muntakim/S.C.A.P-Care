using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;
using SmartClinic.Web.Services.Foundation;

namespace SmartClinic.Web.Controllers;

/// <summary>
/// Read-only JSON for "which slots does this doctor have on this date?".
/// Any signed-in user may call it (patients use it from Doctor Search / Booking in Sprint 1-2).
/// </summary>
[Authorize]
public class AvailabilityController : Controller
{
    private readonly IScheduleService _schedule;
    private readonly UserManager<ApplicationUser> _users;

    public AvailabilityController(IScheduleService schedule, UserManager<ApplicationUser> users)
    {
        _schedule = schedule; _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Slots(string doctorId, DateOnly date)
    {
        var doc = await _users.FindByIdAsync(doctorId);
        if (doc is null || !await _users.IsInRoleAsync(doc, Roles.Doctor)) return NotFound();
        if ((!doc.IsApproved || !doc.IsActive) && !User.IsInRole(Roles.Admin)) return NotFound();

        var today = ClinicTime.Today;
        var day = await _schedule.GetDayScheduleAsync(doctorId, date);
        var nowTime = ClinicTime.TimeNow;
        var slots = day.Slots.Where(s => date > today || (date == today && s.Start > nowTime));

        return Json(new
        {
            date = date.Iso(),
            isPast = date < today,
            onLeave = day.IsOnLeave,
            leaveReason = day.LeaveReason,
            slots = (date < today ? Enumerable.Empty<TimeSlot>() : slots)
                .Select(s => new { start = s.Start.Hm(), end = s.End.Hm() })
        });
    }
}
