using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Data.Foundation;

public abstract class EfRepositoryBase<T> where T : BaseEntity
{
    protected readonly AppDbContext Db;
    protected EfRepositoryBase(AppDbContext db) => Db = db;
    protected DbSet<T> Set => Db.Set<T>();

    public Task<T?> GetAsync(int id) => Set.FirstOrDefaultAsync(e => e.Id == id);
    public void SoftDelete(T entity) => entity.IsDeleted = true;   // global query filter hides it afterwards
    public Task SaveAsync() => Db.SaveChangesAsync();
}

// ------------------------------------------------------------------ shifts
public interface IDoctorShiftRepository
{
    Task<List<DoctorShift>> ListByDoctorAsync(string doctorId);
    Task<DoctorShift?> GetAsync(int id);
    Task<Dictionary<string, int>> CountActiveByDoctorAsync();
    void AddRange(IEnumerable<DoctorShift> shifts);
    void SoftDelete(DoctorShift shift);
    Task SaveAsync();
}

public class DoctorShiftRepository : EfRepositoryBase<DoctorShift>, IDoctorShiftRepository
{
    public DoctorShiftRepository(AppDbContext db) : base(db) { }

    public Task<List<DoctorShift>> ListByDoctorAsync(string doctorId) =>
        Set.Where(s => s.DoctorId == doctorId).OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToListAsync();

    public Task<Dictionary<string, int>> CountActiveByDoctorAsync() =>
        Set.Where(s => s.IsActive).GroupBy(s => s.DoctorId)
           .Select(g => new { g.Key, Count = g.Count() })
           .ToDictionaryAsync(x => x.Key, x => x.Count);

    public void AddRange(IEnumerable<DoctorShift> shifts) => Set.AddRange(shifts);
}

// ------------------------------------------------------------------ leave
public interface IDoctorLeaveRepository
{
    Task<List<DoctorLeave>> ListByDoctorAsync(string doctorId);
    Task<DoctorLeave?> GetAsync(int id);
    void Add(DoctorLeave leave);
    void SoftDelete(DoctorLeave leave);
    Task SaveAsync();
}

public class DoctorLeaveRepository : EfRepositoryBase<DoctorLeave>, IDoctorLeaveRepository
{
    public DoctorLeaveRepository(AppDbContext db) : base(db) { }

    public Task<List<DoctorLeave>> ListByDoctorAsync(string doctorId) =>
        Set.Where(l => l.DoctorId == doctorId).OrderBy(l => l.StartDate).ToListAsync();

    public void Add(DoctorLeave leave) => Set.Add(leave);
}

// ------------------------------------------------------------------ patient profile
public interface IPatientProfileRepository
{
    Task<PatientProfile?> GetByUserIdAsync(string userId);
    Task<PatientProfile> GetOrCreateAsync(string userId);
    Task<List<PatientProfile>> ListByUserIdsAsync(IEnumerable<string> userIds);
    void AddRange(IEnumerable<PatientProfile> profiles);
    Task SaveAsync();
}

public class PatientProfileRepository : EfRepositoryBase<PatientProfile>, IPatientProfileRepository
{
    public PatientProfileRepository(AppDbContext db) : base(db) { }

    public Task<PatientProfile?> GetByUserIdAsync(string userId) =>
        Set.FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task<PatientProfile> GetOrCreateAsync(string userId)
    {
        var existing = await GetByUserIdAsync(userId);
        if (existing is not null) return existing;

        var created = new PatientProfile { UserId = userId };
        Set.Add(created);
        try
        {
            await Db.SaveChangesAsync();
            return created;
        }
        catch (DbUpdateException)
        {
            // two requests raced to create it (unique index on UserId) -> use the winner
            Db.Entry(created).State = EntityState.Detached;
            return await GetByUserIdAsync(userId) ?? throw new InvalidOperationException("Could not create patient profile.");
        }
    }

    public Task<List<PatientProfile>> ListByUserIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        return Set.Where(p => ids.Contains(p.UserId)).ToListAsync();
    }

    public void AddRange(IEnumerable<PatientProfile> profiles) => Set.AddRange(profiles);
}
