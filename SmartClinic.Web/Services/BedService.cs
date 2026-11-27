using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;
using SmartClinic.Web.Repositories;
using SmartClinic.Web.Services.Foundation;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Services;

public interface IBedService
{
    Task<BedIndexViewModel> GetIndexAsync(BedFilter filter, bool canManage);
    Task<BedSummary> GetSummaryAsync();
    Task<BedFormViewModel?> GetFormAsync(int id);
    Task<ServiceResult> CreateAsync(BedFormViewModel model);
    Task<ServiceResult> UpdateAsync(BedFormViewModel model);
    Task<ServiceResult> DeleteAsync(int id);
    Task<AssignBedViewModel?> GetAssignFormAsync(int bedId);
    Task<ServiceResult> AssignAsync(AssignBedViewModel model);
    Task<ServiceResult> DischargeAsync(int bedId);
    Task<ServiceResult> SetMaintenanceAsync(int bedId, bool maintenance);
}

public class BedService : IBedService
{
    private const string Stale = "This bed was just changed by someone else. Please refresh and try again.";
    private readonly IBedRepository _beds;
    private readonly UserManager<ApplicationUser> _users;

    public BedService(IBedRepository beds, UserManager<ApplicationUser> users)
    {
        _beds = beds; _users = users;
    }

    public async Task<BedIndexViewModel> GetIndexAsync(BedFilter f, bool canManage)
    {
        var beds = await _beds.SearchAsync(f.Type, f.Status, f.Ward, f.Q);
        var active = await _beds.ActiveAssignmentsAsync();

        var items = beds.Select(b =>
        {
            active.TryGetValue(b.Id, out var a);
            return new BedListItem
            {
                Id = b.Id, BedNumber = b.BedNumber, Ward = b.Ward, Type = b.Type, DailyRate = b.DailyRate,
                Status = b.Status, Notes = b.Notes,
                PatientName = a?.Patient?.FullName, PatientUserName = a?.Patient?.UserName, AdmittedAt = a?.AdmittedAt
            };
        }).ToList();

        return new BedIndexViewModel
        {
            Filter = f, Items = items, CanManage = canManage,
            Wards = await _beds.WardsAsync(), Summary = await _beds.GetSummaryAsync()
        };
    }

    public Task<BedSummary> GetSummaryAsync() => _beds.GetSummaryAsync();

    public async Task<BedFormViewModel?> GetFormAsync(int id)
    {
        var b = await _beds.GetAsync(id);
        return b is null ? null : new BedFormViewModel
        {
            Id = b.Id, BedNumber = b.BedNumber, Ward = b.Ward, Type = b.Type, DailyRate = b.DailyRate, Notes = b.Notes
        };
    }

    public async Task<ServiceResult> CreateAsync(BedFormViewModel m)
    {
        var number = m.BedNumber.Trim();
        if (await _beds.NumberExistsAsync(number, null))
            return ServiceResult.Fail($"'{number}' already exists. Use a different bed/cabin number.");

        _beds.Add(new Bed
        {
            BedNumber = number, Ward = m.Ward.Trim(), Type = m.Type, DailyRate = m.DailyRate, Notes = Clean(m.Notes)
        });
        try { await _beds.SaveAsync(); }
        catch (DbUpdateException) { return ServiceResult.Fail($"'{number}' already exists. Use a different bed/cabin number."); }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(BedFormViewModel m)
    {
        if (m.Id is null) return ServiceResult.Fail("Bed not found.");
        var bed = await _beds.GetAsync(m.Id.Value);
        if (bed is null) return ServiceResult.Fail("Bed not found.");

        var number = m.BedNumber.Trim();
        if (await _beds.NumberExistsAsync(number, bed.Id))
            return ServiceResult.Fail($"'{number}' already exists. Use a different bed/cabin number.");

        bed.BedNumber = number; bed.Ward = m.Ward.Trim(); bed.Type = m.Type;
        bed.DailyRate = m.DailyRate; bed.Notes = Clean(m.Notes);
        try { await _beds.SaveAsync(); }
        catch (DbUpdateException) { return ServiceResult.Fail(Stale); }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var bed = await _beds.GetAsync(id);
        if (bed is null) return ServiceResult.Fail("Bed not found.");
        if (bed.Status == BedStatus.Occupied) return ServiceResult.Fail("An occupied bed cannot be deleted. Discharge the patient first.");

        _beds.SoftDelete(bed);
        try { await _beds.SaveAsync(); }
        catch (DbUpdateException) { return ServiceResult.Fail(Stale); }
        return ServiceResult.Ok();
    }

    public async Task<AssignBedViewModel?> GetAssignFormAsync(int bedId)
    {
        var bed = await _beds.GetAsync(bedId);
        if (bed is null) return null;
        return new AssignBedViewModel
        {
            BedId = bed.Id, BedNumber = bed.BedNumber, Ward = bed.Ward, Type = bed.Type,
            Patients = await PatientOptionsAsync()
        };
    }

    public async Task<ServiceResult> AssignAsync(AssignBedViewModel m)
    {
        var bed = await _beds.GetAsync(m.BedId);
        if (bed is null) return ServiceResult.Fail("Bed not found.");
        if (bed.Status == BedStatus.Maintenance) return ServiceResult.Fail("This bed is under maintenance.");
        if (bed.Status == BedStatus.Occupied) return ServiceResult.Fail("This bed is already occupied.");

        var patient = m.PatientId is null ? null : await _users.FindByIdAsync(m.PatientId);
        if (patient is null || !patient.IsActive || !await _users.IsInRoleAsync(patient, Roles.Patient))
            return ServiceResult.Fail("Select a valid, active patient.");
        if (await _beds.PatientHasActiveBedAsync(patient.Id))
            return ServiceResult.Fail($"{patient.FullName} is already admitted to another bed.");

        bed.Status = BedStatus.Occupied;
        _beds.AddAssignment(new BedAssignment
        {
            BedId = bed.Id, PatientId = patient.Id, AdmittedAt = ClinicTime.Now, Notes = Clean(m.Notes)
        });
        try { await _beds.SaveAsync(); }
        catch (DbUpdateException) { return ServiceResult.Fail(Stale); }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DischargeAsync(int bedId)
    {
        var bed = await _beds.GetAsync(bedId);
        if (bed is null) return ServiceResult.Fail("Bed not found.");
        if (bed.Status != BedStatus.Occupied) return ServiceResult.Fail("This bed is not occupied.");

        var assignment = await _beds.GetActiveAssignmentAsync(bedId);
        if (assignment is not null) assignment.DischargedAt = ClinicTime.Now;
        bed.Status = BedStatus.Available;
        try { await _beds.SaveAsync(); }
        catch (DbUpdateException) { return ServiceResult.Fail(Stale); }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetMaintenanceAsync(int bedId, bool maintenance)
    {
        var bed = await _beds.GetAsync(bedId);
        if (bed is null) return ServiceResult.Fail("Bed not found.");

        if (maintenance)
        {
            if (bed.Status == BedStatus.Occupied) return ServiceResult.Fail("Discharge the patient before putting this bed under maintenance.");
            bed.Status = BedStatus.Maintenance;
        }
        else
        {
            if (bed.Status != BedStatus.Maintenance) return ServiceResult.Fail("This bed is not under maintenance.");
            bed.Status = BedStatus.Available;
        }
        try { await _beds.SaveAsync(); }
        catch (DbUpdateException) { return ServiceResult.Fail(Stale); }
        return ServiceResult.Ok();
    }

    private async Task<List<SelectListItem>> PatientOptionsAsync()
    {
        var admitted = await _beds.PatientIdsWithActiveBedAsync();
        var patients = await _users.GetUsersInRoleAsync(Roles.Patient);
        return patients.Where(p => p.IsActive && !admitted.Contains(p.Id))
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem($"{p.FullName} ({p.UserName})", p.Id)).ToList();
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
