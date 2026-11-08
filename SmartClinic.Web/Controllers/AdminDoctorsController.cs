using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Services.Foundation;

namespace SmartClinic.Web.Controllers;

/// <summary>Sprint 1 / Member A - admin lists doctors, activates/deactivates them and jumps to their schedule.</summary>
[Authorize(Roles = Roles.Admin)]
public class AdminDoctorsController : Controller
{
    private readonly IDoctorAdminService _doctors;
    public AdminDoctorsController(IDoctorAdminService doctors) => _doctors = doctors;

    public async Task<IActionResult> Index() => View(await _doctors.ListDoctorsAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool active)
    {
        var result = await _doctors.SetActiveAsync(id, active);
        if (result.Success) TempData["Success"] = active ? "Doctor activated." : "Doctor deactivated.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index));
    }
}
