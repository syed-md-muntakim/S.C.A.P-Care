using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Services;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

/// <summary>Sprint 1 #20: Bed &amp; Cabin availability. Admin manages; doctors can view availability.</summary>
[Authorize(Roles = Roles.Admin + "," + Roles.Doctor)]
public class BedsController : Controller
{
    private readonly IBedService _beds;
    public BedsController(IBedService beds) => _beds = beds;

    public async Task<IActionResult> Index([FromQuery] BedFilter filter) =>
        View(await _beds.GetIndexAsync(filter, User.IsInRole(Roles.Admin)));

    // ---------------------------------------------------------------- create / edit / delete
    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View(new BedFormViewModel());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(BedFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _beds.CreateAsync(model);
        if (!result.Success) { ModelState.AddModelError(nameof(model.BedNumber), result.Error!); return View(model); }
        TempData["Success"] = $"{model.Type} {model.BedNumber.Trim()} added.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _beds.GetFormAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(BedFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _beds.UpdateAsync(model);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error!); return View(model); }
        TempData["Success"] = "Bed details updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id) => Report(await _beds.DeleteAsync(id), "Bed removed.");

    // ---------------------------------------------------------------- assign / discharge / maintenance
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Assign(int id)
    {
        var model = await _beds.GetAssignFormAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Assign(AssignBedViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _beds.AssignAsync(model);
            if (result.Success)
            {
                TempData["Success"] = $"Patient admitted to {model.BedNumber}.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        var fresh = await _beds.GetAssignFormAsync(model.BedId);
        if (fresh is null) return NotFound();
        model.Patients = fresh.Patients;
        model.BedNumber = fresh.BedNumber; model.Ward = fresh.Ward; model.Type = fresh.Type;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Discharge(int id) => Report(await _beds.DischargeAsync(id), "Patient discharged. The bed is available again.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Maintenance(int id, bool maintenance) =>
        Report(await _beds.SetMaintenanceAsync(id, maintenance), maintenance ? "Bed marked as under maintenance." : "Bed is available again.");

    private IActionResult Report(Services.Foundation.ServiceResult r, string okMessage)
    {
        if (r.Success) TempData["Success"] = okMessage; else TempData["Error"] = r.Error;
        return RedirectToAction(nameof(Index));
    }
}
