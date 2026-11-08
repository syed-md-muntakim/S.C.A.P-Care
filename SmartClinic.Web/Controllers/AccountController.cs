using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Services;

namespace SmartClinic.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAppEmailSender _email;
    private readonly IWebHostEnvironment _env;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        IAppEmailSender email, IWebHostEnvironment env)
    {
        _users = users; _signIn = signIn; _email = email; _env = env;
    }

    // ---------- Login ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectByRole(User.IsInRole(Roles.Admin) ? Roles.Admin : User.IsInRole(Roles.Doctor) ? Roles.Doctor : Roles.Patient);
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByNameAsync(model.UserId.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid User ID or password.");
            return View(model);
        }

        var result = await _signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked for 5 minutes after too many failed attempts.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid User ID or password.");
            return View(model);
        }
        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact the administrator.");
            return View(model);
        }
        if (!user.IsApproved)
        {
            ModelState.AddModelError(string.Empty, "Your doctor account is awaiting admin approval.");
            return View(model);
        }

        await _signIn.SignInAsync(user, model.RememberMe);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

        var roles = await _users.GetRolesAsync(user);
        return RedirectByRole(roles.FirstOrDefault() ?? Roles.Patient);
    }

    // ---------- Register (Doctor / Patient) ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Register(string? role = null) =>
        View(new RegisterViewModel { Role = role == Roles.Doctor ? Roles.Doctor : Roles.Patient });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role != Roles.Doctor && model.Role != Roles.Patient)
            ModelState.AddModelError(nameof(model.Role), "Choose a valid account type.");
        if (model.Role == Roles.Doctor && string.IsNullOrWhiteSpace(model.Specialty))
            ModelState.AddModelError(nameof(model.Specialty), "Specialty is required for doctors.");
        if (model.UserId.Equals("admin", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.UserId), "This User ID is reserved.");
        if (!ModelState.IsValid) return View(model);

        var isDoctor = model.Role == Roles.Doctor;
        var user = new ApplicationUser
        {
            UserName = model.UserId.Trim(),
            Email = model.Email.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            FullName = model.FullName.Trim(),
            Specialty = isDoctor ? model.Specialty!.Trim() : null,
            IsApproved = !isDoctor,
            EmailConfirmed = true // email confirmation flow can be added later
        };

        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        await _users.AddToRoleAsync(user, model.Role);

        if (isDoctor)
        {
            TempData["Success"] = "Registration successful. You can sign in once the admin approves your account.";
            return RedirectToAction(nameof(Login));
        }
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectByRole(Roles.Patient);
    }

    // ---------- Logout ----------
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---------- Forgot / Reset password ----------
    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme)!;
            await _email.SendAsync(user.Email!, "Reset your Smart Clinic password",
                $"<p>Hello {user.FullName},</p><p><a href=\"{link}\">Click here to reset your password</a>.</p>");
            if (_env.IsDevelopment()) TempData["DevResetLink"] = link; // no SMTP yet: show link in dev only
        }
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (email is null || token is null) return RedirectToAction(nameof(Login));
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user is null) return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _users.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(model);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectByRole(string role) => role switch
    {
        Roles.Admin => RedirectToAction("Dashboard", "Admin"),
        Roles.Doctor => RedirectToAction("Dashboard", "Doctor"),
        _ => RedirectToAction("Dashboard", "Patient")
    };
}
