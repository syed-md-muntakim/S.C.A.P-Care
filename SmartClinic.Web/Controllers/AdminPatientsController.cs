using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Services.Foundation;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminPatientsController : Controller
{
    private readonly IPatientAccountService _patients;
    public AdminPatientsController(IPatientAccountService patients) => _patients = patients;

    public async Task<IActionResult> Index() => View(await _patients.ListPatientsAsync());

    public async Task<IActionResult> Details(string id)
    {
        var model = await _patients.GetAccountAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool active, string? returnTo)
    {
        var result = await _patients.SetActiveAsync(id, active);
        if (result.Success) TempData["Success"] = active ? "Patient account activated." : "Patient account deactivated.";
        else TempData["Error"] = result.Error;
        return returnTo == "details" ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Index));
    }
}
