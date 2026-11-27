using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Services;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

/// <summary>Sprint 2 #4: queue and token management with live status (SignalR).</summary>
[Authorize]
public class QueueController : Controller
{
    private const string Staff = Roles.Admin + "," + Roles.Doctor;
    private readonly IQueueService _queue;
    private readonly IAppointmentService _appts;

    public QueueController(IQueueService queue, IAppointmentService appts) { _queue = queue; _appts = appts; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ---------------------------------------------------------------- staff console (doctor controls, admin watches / checks in)
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Index(string? doctorId)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var vm = new QueueIndexViewModel { IsAdmin = isAdmin };
        if (isAdmin)
        {
            vm.Doctors = await _appts.DoctorOptionsAsync();
            doctorId ??= vm.Doctors.FirstOrDefault()?.Value;
        }
        else doctorId = UserId;

        vm.SelectedDoctorId = doctorId;
        if (doctorId is not null) vm.Queue = await _queue.GetDoctorQueueAsync(doctorId, canControl: !isAdmin);
        return View(vm);
    }

    [Authorize(Roles = Staff)]
    public async Task<IActionResult> ConsolePartial(string? doctorId)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        doctorId = isAdmin ? doctorId : UserId;
        if (doctorId is null) return NoContent();
        var vm = await _queue.GetDoctorQueueAsync(doctorId, canControl: !isAdmin);
        return vm is null ? NotFound() : PartialView("_Console", vm);
    }

    // ---------------------------------------------------------------- check-in: patient (own), doctor or admin
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(int appointmentId, string? doctorId)
    {
        var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Doctor);
        var result = await _queue.IssueTokenAsync(appointmentId, UserId, isStaff);
        if (result.Success) TempData["Success"] = $"Token #{result.TokenNumber} issued.";
        else TempData["Error"] = result.Error;

        if (!isStaff) return RedirectToAction(nameof(MyToken));
        return RedirectToAction(nameof(Index), new { doctorId });
    }

    // ---------------------------------------------------------------- doctor actions
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> CallNext()
    {
        var r = await _queue.CallNextAsync(UserId);
        if (r.Success) TempData["Success"] = $"Now serving token #{r.TokenNumber}."; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> Complete(int id) => Report(await _queue.CompleteAsync(id, UserId), "Consultation completed.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> Skip(int id) => Report(await _queue.SkipAsync(id, UserId), "Token skipped.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Doctor)]
    public async Task<IActionResult> Requeue(int id) => Report(await _queue.RequeueAsync(id, UserId), "Token returned to the queue.");

    // ---------------------------------------------------------------- patient: my token
    [Authorize(Roles = Roles.Patient)]
    public async Task<IActionResult> MyToken() => View(await _queue.GetMyQueueAsync(UserId));

    [Authorize(Roles = Roles.Patient)]
    public async Task<IActionResult> MyTokenPartial() => PartialView("_MyToken", await _queue.GetMyQueueAsync(UserId));

    // ---------------------------------------------------------------- public board (any signed-in user)
    public async Task<IActionResult> Board() => View(await _queue.GetBoardAsync());
    public async Task<IActionResult> BoardPartial() => PartialView("_Board", await _queue.GetBoardAsync());

    private IActionResult Report(TokenResult r, string okMessage)
    {
        if (r.Success) TempData["Success"] = okMessage; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }
}
