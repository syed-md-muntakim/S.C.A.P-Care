using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Repositories;

public interface IQueueRepository
{
    Task<List<QueueToken>> ForDoctorAsync(string doctorId, DateOnly date);
    Task<List<QueueToken>> ForDateAsync(DateOnly date);
    Task<QueueToken?> GetAsync(int id);
    Task<QueueToken?> NextWaitingAsync(string doctorId, DateOnly date);
    Task<int> NextNumberAsync(string doctorId, DateOnly date);
    Task<bool> HasInConsultationAsync(string doctorId, DateOnly date);
    Task<bool> ExistsForAppointmentAsync(int appointmentId);
    Task<int> CountWaitingAsync(DateOnly date, string? doctorId);
    void Add(QueueToken token);
    void Detach(QueueToken token);
    Task SaveAsync();
}

public class QueueRepository : IQueueRepository
{
    private readonly AppDbContext _db;
    public QueueRepository(AppDbContext db) => _db = db;

    public Task<List<QueueToken>> ForDoctorAsync(string doctorId, DateOnly date) =>
        _db.QueueTokens.AsNoTracking().Include(t => t.Patient).Include(t => t.Appointment)
           .Where(t => t.DoctorId == doctorId && t.QueueDate == date)
           .OrderBy(t => t.TokenNumber).ToListAsync();

    public Task<List<QueueToken>> ForDateAsync(DateOnly date) =>
        _db.QueueTokens.AsNoTracking().Where(t => t.QueueDate == date).OrderBy(t => t.TokenNumber).ToListAsync();

    public Task<QueueToken?> GetAsync(int id) =>
        _db.QueueTokens.Include(t => t.Appointment).FirstOrDefaultAsync(t => t.Id == id);

    public Task<QueueToken?> NextWaitingAsync(string doctorId, DateOnly date) =>
        _db.QueueTokens.Where(t => t.DoctorId == doctorId && t.QueueDate == date && t.Status == TokenStatus.Waiting)
           .OrderBy(t => t.TokenNumber).FirstOrDefaultAsync();

    public async Task<int> NextNumberAsync(string doctorId, DateOnly date)
    {
        // IgnoreQueryFilters: never reuse the number of a soft-deleted token (unique index covers all rows).
        var max = await _db.QueueTokens.IgnoreQueryFilters()
            .Where(t => t.DoctorId == doctorId && t.QueueDate == date)
            .MaxAsync(t => (int?)t.TokenNumber);
        return (max ?? 0) + 1;
    }

    public Task<bool> HasInConsultationAsync(string doctorId, DateOnly date) =>
        _db.QueueTokens.AnyAsync(t => t.DoctorId == doctorId && t.QueueDate == date && t.Status == TokenStatus.InConsultation);

    public Task<bool> ExistsForAppointmentAsync(int appointmentId) =>
        _db.QueueTokens.AnyAsync(t => t.AppointmentId == appointmentId);

    public Task<int> CountWaitingAsync(DateOnly date, string? doctorId) =>
        _db.QueueTokens.CountAsync(t => t.QueueDate == date && t.Status == TokenStatus.Waiting
                                        && (doctorId == null || t.DoctorId == doctorId));

    public void Add(QueueToken token) => _db.QueueTokens.Add(token);
    public void Detach(QueueToken token) => _db.Entry(token).State = EntityState.Detached;
    public Task SaveAsync() => _db.SaveChangesAsync();
}
