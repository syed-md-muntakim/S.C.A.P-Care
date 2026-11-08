using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Services;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = Roles.Patient + "," + Roles.Admin)]
public class DoctorsController : Controller
{
    private readonly IDoctorSearchService _search;
    public DoctorsController(IDoctorSearchService search) => _search = search;

    // GET /Doctors?Keyword=&Specialty=&Sort=&Page=
    public async Task<IActionResult> Index([FromQuery] DoctorSearchQuery query) =>
        View(await _search.SearchAsync(query));

    // GET /Doctors/Details/{id}
    public async Task<IActionResult> Details(string id)
    {
        var doctor = await _search.GetAsync(id);
        return doctor is null ? NotFound() : View(doctor);
    }
}
