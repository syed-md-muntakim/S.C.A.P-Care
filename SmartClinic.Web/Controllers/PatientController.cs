using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = Roles.Patient)]
public class PatientController : Controller
{
    public IActionResult Dashboard() => View();
}
