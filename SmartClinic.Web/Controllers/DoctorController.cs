using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = Roles.Doctor)]
public class DoctorController : Controller
{
    public IActionResult Dashboard() => View();
}
