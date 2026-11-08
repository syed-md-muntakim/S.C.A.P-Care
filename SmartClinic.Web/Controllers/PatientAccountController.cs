using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;
using SmartClinic.Web.Services.Foundation;

namespace SmartClinic.Web.Controllers;


[Authorize(Roles = Roles.Patient)]
public class PatientAccountController : Controller
{
    private static readonly string[] Genders = { "Male", "Female", "Other" };
    private static readonly string[] BloodGroups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    private readonly IPatientAccountService _accounts;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public PatientAccountController(IPatientAccountService accounts, SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _accounts = accounts; _signIn = signIn; _users = users;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;


    public async Task<IActionResult> Index()
    {
        var model = await _accounts.GetAccountAsync(CurrentUserId);
        return model is null ? NotFound() : View(model);
    }


    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var account = await _accounts.GetAccountAsync(CurrentUserId);
        return account is null ? NotFound() : View(EditPatientAccountViewModel.From(account));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditPatientAccountViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Gender) && !Genders.Contains(model.Gender))
            ModelState.AddModelError(nameof(model.Gender), "Choose a valid gender.");
        if (!string.IsNullOrWhiteSpace(model.BloodGroup) && !BloodGroups.Contains(model.BloodGroup))
            ModelState.AddModelError(nameof(model.BloodGroup), "Choose a valid blood group.");
        if (!ModelState.IsValid) return View(model);

        var result = await _accounts.UpdateAsync(CurrentUserId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your profile.");
            return View(model);
        }
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _accounts.ChangePasswordAsync(CurrentUserId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not change the password.");
            return View(new ChangePasswordViewModel());
        }

    
        var user = await _users.FindByIdAsync(CurrentUserId);
        if (user is not null) await _signIn.RefreshSignInAsync(user);

        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Deactivate() => View(new DeactivateAccountViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(DeactivateAccountViewModel model)
    {
        if (!ModelState.IsValid) return View(new DeactivateAccountViewModel());

        var result = await _accounts.DeactivateAsync(CurrentUserId, model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(nameof(model.Password), result.Error ?? "Could not deactivate the account.");
            return View(new DeactivateAccountViewModel());
        }

        await _signIn.SignOutAsync();
        TempData["Success"] = "Your account has been deactivated. Contact the clinic administrator to reactivate it.";
        return RedirectToAction("Login", "Account");
    }
}
