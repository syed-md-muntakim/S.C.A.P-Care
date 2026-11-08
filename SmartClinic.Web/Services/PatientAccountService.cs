using Microsoft.AspNetCore.Identity;
using SmartClinic.Web.Common;
using SmartClinic.Web.Data.Foundation;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Services.Foundation;

public interface IPatientAccountService
{
    Task<PatientAccountViewModel?> GetAccountAsync(string userId);
    Task<ServiceResult> UpdateAsync(string userId, EditPatientAccountViewModel model);
    Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordViewModel model);
    Task<ServiceResult> DeactivateAsync(string userId, string password);

   
    Task<IReadOnlyList<PatientListItem>> ListPatientsAsync();
    Task<ServiceResult> SetActiveAsync(string userId, bool active);
}

public class PatientAccountService : IPatientAccountService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPatientProfileRepository _profiles;

    public PatientAccountService(UserManager<ApplicationUser> users, IPatientProfileRepository profiles)
    {
        _users = users; _profiles = profiles;
    }

    public async Task<PatientAccountViewModel?> GetAccountAsync(string userId)
    {
        var user = await FindPatientAsync(userId);
        if (user is null) return null;
        var profile = await _profiles.GetOrCreateAsync(user.Id);
        return Map(user, profile);
    }

    public async Task<ServiceResult> UpdateAsync(string userId, EditPatientAccountViewModel m)
    {
        if (m.DateOfBirth is { } dob && (dob > ClinicTime.Today || dob.Year < 1900))
            return ServiceResult.Fail("Date of birth is not valid.");

        var user = await FindPatientAsync(userId);
        if (user is null) return ServiceResult.Fail("Account not found.");

        var email = m.Email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var r = await _users.SetEmailAsync(user, email);   // validates uniqueness
            if (!r.Succeeded) return Fail(r);
        }
        var phone = m.PhoneNumber.Trim();
        if (user.PhoneNumber != phone)
        {
            var r = await _users.SetPhoneNumberAsync(user, phone);
            if (!r.Succeeded) return Fail(r);
        }
        user.FullName = m.FullName.Trim();
        user.EmailConfirmed = true;   // the app has no e-mail confirmation flow yet (same as Register)
        var saved = await _users.UpdateAsync(user);
        if (!saved.Succeeded) return Fail(saved);

        var p = await _profiles.GetOrCreateAsync(user.Id);
        p.DateOfBirth = m.DateOfBirth;
        p.Gender = Clean(m.Gender);
        p.BloodGroup = Clean(m.BloodGroup);
        p.Address = Clean(m.Address);
        p.EmergencyContactName = Clean(m.EmergencyContactName);
        p.EmergencyContactPhone = Clean(m.EmergencyContactPhone);
        p.Allergies = Clean(m.Allergies);
        p.ChronicConditions = Clean(m.ChronicConditions);
        await _profiles.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordViewModel m)
    {
        var user = await FindPatientAsync(userId);
        if (user is null) return ServiceResult.Fail("Account not found.");
        var r = await _users.ChangePasswordAsync(user, m.CurrentPassword, m.NewPassword);
        return r.Succeeded ? ServiceResult.Ok() : Fail(r);
    }

    public async Task<ServiceResult> DeactivateAsync(string userId, string password)
    {
        var user = await FindPatientAsync(userId);
        if (user is null) return ServiceResult.Fail("Account not found.");
        if (!await _users.CheckPasswordAsync(user, password)) return ServiceResult.Fail("Incorrect password.");

        user.IsActive = false;
        var r = await _users.UpdateAsync(user);
        if (!r.Succeeded) return Fail(r);
        await _users.UpdateSecurityStampAsync(user);   // invalidates other signed-in sessions
        return ServiceResult.Ok();
    }

    // ------------------------------------------------------------------ admin
    public async Task<IReadOnlyList<PatientListItem>> ListPatientsAsync()
    {
        var patients = await _users.GetUsersInRoleAsync(Roles.Patient);
        var map = (await _profiles.ListByUserIdsAsync(patients.Select(p => p.Id))).ToDictionary(p => p.UserId);

        // patients who registered but never opened "My Account" have no profile row yet -> create them so every patient has a PT- code
        var missing = patients.Where(p => !map.ContainsKey(p.Id)).Select(p => new PatientProfile { UserId = p.Id }).ToList();
        if (missing.Count > 0)
        {
            _profiles.AddRange(missing);
            await _profiles.SaveAsync();
            foreach (var m in missing) map[m.UserId] = m;
        }

        return patients.OrderBy(p => p.FullName)
            .Select(p => new PatientListItem(p.Id, p.UserName ?? "", p.FullName, p.Email, p.PhoneNumber,
                map[p.Id].PatientCode, p.CreatedAt, p.IsActive))
            .ToList();
    }

    public async Task<ServiceResult> SetActiveAsync(string userId, bool active)
    {
        var user = await FindPatientAsync(userId);
        if (user is null) return ServiceResult.Fail("Patient not found.");
        user.IsActive = active;
        var r = await _users.UpdateAsync(user);
        if (!r.Succeeded) return Fail(r);
        if (!active) await _users.UpdateSecurityStampAsync(user);
        return ServiceResult.Ok();
    }

    // ------------------------------------------------------------------ helpers
    private async Task<ApplicationUser?> FindPatientAsync(string userId)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return null;
        return await _users.IsInRoleAsync(user, Roles.Patient) ? user : null;
    }

    private static PatientAccountViewModel Map(ApplicationUser u, PatientProfile p)
    {
        int? age = null;
        if (p.DateOfBirth is { } dob)
        {
            var today = ClinicTime.Today;
            age = today.Year - dob.Year - (today < dob.AddYears(today.Year - dob.Year) ? 1 : 0);
        }
        return new PatientAccountViewModel
        {
            UserId = u.Id, UserName = u.UserName ?? "", PatientCode = p.PatientCode, FullName = u.FullName,
            Email = u.Email, PhoneNumber = u.PhoneNumber, DateOfBirth = p.DateOfBirth, Age = age,
            Gender = p.Gender, BloodGroup = p.BloodGroup, Address = p.Address,
            EmergencyContactName = p.EmergencyContactName, EmergencyContactPhone = p.EmergencyContactPhone,
            Allergies = p.Allergies, ChronicConditions = p.ChronicConditions,
            MemberSince = u.CreatedAt, IsActive = u.IsActive
        };
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static ServiceResult Fail(IdentityResult r) => ServiceResult.Fail(string.Join(" ", r.Errors.Select(e => e.Description)));
}
