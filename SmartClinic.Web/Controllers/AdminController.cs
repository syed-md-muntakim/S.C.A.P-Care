using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Services;


namespace SmartClinic.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IMedicineInventoryService _medicineInventory;

    public AdminController(
        UserManager<ApplicationUser> users,
        IMedicineInventoryService medicineInventory)
    {
        _users = users;
        _medicineInventory = medicineInventory;
    }


    public async Task<IActionResult> Dashboard()
    {
        var doctors = await _users.GetUsersInRoleAsync(Roles.Doctor);
        var patients = await _users.GetUsersInRoleAsync(Roles.Patient);
        var inventory = await _medicineInventory.GetSummaryAsync();
        
        return View(new AdminDashboardViewModel
        {
            ApprovedDoctors = doctors.Count(d => d.IsApproved),
            TotalPatients = patients.Count,
            PendingDoctors = doctors.Where(d => !d.IsApproved).OrderBy(d => d.CreatedAt).ToList(),
            TotalMedicines = inventory.TotalMedicines,
            LowStockMedicines = inventory.LowStockMedicines,
            OutOfStockMedicines = inventory.OutOfStockMedicines,
            ExpiringSoonMedicines = inventory.ExpiringSoonMedicines,
            ExpiredMedicines = inventory.ExpiredMedicines

        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDoctor(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is not null && await _users.IsInRoleAsync(user, Roles.Doctor))
        {
            user.IsApproved = true;
            await _users.UpdateAsync(user);
            TempData["Success"] = $"Dr. {user.FullName} approved.";
        }
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDoctor(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is not null && !user.IsApproved && await _users.IsInRoleAsync(user, Roles.Doctor))
        {
            await _users.DeleteAsync(user);
            TempData["Success"] = "Doctor registration rejected and removed.";
        }
        return RedirectToAction(nameof(Dashboard));
    }
}
