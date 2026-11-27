using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Repositories;

public interface IBedRepository
{
    Task<List<Bed>> SearchAsync(BedType? type, BedStatus? status, string? ward, string? q);
    Task<Bed?> GetAsync(int id);
    Task<bool> NumberExistsAsync(string number, int? excludeId);
    Task<List<string>> WardsAsync();
    Task<Dictionary<int, BedAssignment>> ActiveAssignmentsAsync();
    Task<BedAssignment?> GetActiveAssignmentAsync(int bedId);
    Task<bool> PatientHasActiveBedAsync(string patientId);
    Task<HashSet<string>> PatientIdsWithActiveBedAsync();
    Task<BedSummary> GetSummaryAsync();
    Task<bool> AnyAsync();
    void Add(Bed bed);
    void AddRange(IEnumerable<Bed> beds);
    void AddAssignment(BedAssignment assignment);
    void SoftDelete(Bed bed);
    Task SaveAsync();
}

public class BedRepository : IBedRepository
{
    private readonly AppDbContext _db;
    public BedRepository(AppDbContext db) => _db = db;

    public Task<List<Bed>> SearchAsync(BedType? type, BedStatus? status, string? ward, string? q)
    {
        var query = _db.Beds.AsNoTracking().AsQueryable();
        if (type is not null) query = query.Where(b => b.Type == type);
        if (status is not null) query = query.Where(b => b.Status == status);
        if (!string.IsNullOrWhiteSpace(ward)) query = query.Where(b => b.Ward == ward);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(b => b.BedNumber.Contains(term) || b.Ward.Contains(term));
        }
        return query.OrderBy(b => b.Ward).ThenBy(b => b.BedNumber).ToListAsync();
    }

    public Task<Bed?> GetAsync(int id) => _db.Beds.FirstOrDefaultAsync(b => b.Id == id);

    public Task<bool> NumberExistsAsync(string number, int? excludeId) =>
        _db.Beds.AnyAsync(b => b.BedNumber == number && (excludeId == null || b.Id != excludeId));

    public Task<List<string>> WardsAsync() => _db.Beds.Select(b => b.Ward).Distinct().OrderBy(w => w).ToListAsync();

    public Task<Dictionary<int, BedAssignment>> ActiveAssignmentsAsync() =>
        _db.BedAssignments.AsNoTracking().Include(a => a.Patient)
           .Where(a => a.DischargedAt == null).ToDictionaryAsync(a => a.BedId);

    public Task<BedAssignment?> GetActiveAssignmentAsync(int bedId) =>
        _db.BedAssignments.FirstOrDefaultAsync(a => a.BedId == bedId && a.DischargedAt == null);

    public Task<bool> PatientHasActiveBedAsync(string patientId) =>
        _db.BedAssignments.AnyAsync(a => a.PatientId == patientId && a.DischargedAt == null);

    public async Task<HashSet<string>> PatientIdsWithActiveBedAsync() =>
        (await _db.BedAssignments.Where(a => a.DischargedAt == null).Select(a => a.PatientId).ToListAsync()).ToHashSet();

    public async Task<BedSummary> GetSummaryAsync()
    {
        var all = await _db.Beds.AsNoTracking().Select(b => new { b.Type, b.Status }).ToListAsync();
        return new BedSummary(
            all.Count(b => b.Type == BedType.Bed),
            all.Count(b => b.Type == BedType.Bed && b.Status == BedStatus.Available),
            all.Count(b => b.Type == BedType.Cabin),
            all.Count(b => b.Type == BedType.Cabin && b.Status == BedStatus.Available),
            all.Count(b => b.Status == BedStatus.Occupied),
            all.Count(b => b.Status == BedStatus.Maintenance));
    }

    public Task<bool> AnyAsync() => _db.Beds.AnyAsync();
    public void Add(Bed bed) => _db.Beds.Add(bed);
    public void AddRange(IEnumerable<Bed> beds) => _db.Beds.AddRange(beds);
    public void AddAssignment(BedAssignment assignment) => _db.BedAssignments.Add(assignment);
    public void SoftDelete(Bed bed) => bed.IsDeleted = true;
    public Task SaveAsync() => _db.SaveChangesAsync();
}
