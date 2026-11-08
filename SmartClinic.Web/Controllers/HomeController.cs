using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;

namespace SmartClinic.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(Roles.Admin)) return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole(Roles.Doctor)) return RedirectToAction("Dashboard", "Doctor");
            return RedirectToAction("Dashboard", "Patient");
        }
        return View();
    }

    public IActionResult Error() => View(model: Activity.Current?.Id ?? HttpContext.TraceIdentifier);
}
