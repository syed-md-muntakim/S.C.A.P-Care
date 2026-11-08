using Microsoft.AspNetCore.Identity;
using SmartClinic.Web.Common;
using SmartClinic.Web.Data.Foundation;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Services.Foundation;


public interface IDoctorAdminService
{
    Task<IReadOnlyList<DoctorListItem>> ListDoctorsAsync();
    Task<ServiceResult> SetActiveAsync(string doctorId, bool active);
}

public class DoctorAdminService : IDoctorAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IDoctorShiftRepository _shifts;

    public DoctorAdminService(UserManager<ApplicationUser> users, IDoctorShiftRepository shifts)
    {
        _users = users; _shifts = shifts;
    }

    public async Task<IReadOnlyList<DoctorListItem>> ListDoctorsAsync()
    {
        var doctors = await _users.GetUsersInRoleAsync(Roles.Doctor);
        var counts = await _shifts.CountActiveByDoctorAsync();
        return doctors.OrderBy(d => d.FullName)
            .Select(d => new DoctorListItem(d.Id, d.UserName ?? "", d.FullName, d.Specialty, d.Email, d.PhoneNumber,
                d.IsApproved, d.IsActive, counts.TryGetValue(d.Id, out var c) ? c : 0))
            .ToList();
    }

    public async Task<ServiceResult> SetActiveAsync(string doctorId, bool active)
    {
        var user = await _users.FindByIdAsync(doctorId);
        if (user is null || !await _users.IsInRoleAsync(user, Roles.Doctor)) return ServiceResult.Fail("Doctor not found.");
        user.IsActive = active;
        var r = await _users.UpdateAsync(user);
        if (!r.Succeeded) return ServiceResult.Fail(string.Join(" ", r.Errors.Select(e => e.Description)));
        if (!active) await _users.UpdateSecurityStampAsync(user);
        return ServiceResult.Ok();
    }
}
