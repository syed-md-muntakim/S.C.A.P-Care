using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Repositories;

public class AppointmentFilter
{
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public AppointmentStatus? Status { get; set; }
    public bool ExcludeCancelled { get; set; }
    public bool Descending { get; set; }
    public int Take { get; set; } = 200;
}

public interface IAppointmentRepository
{
    Task<Appointment?> GetAsync(int id);
    Task<HashSet<TimeOnly>> BookedStartsAsync(string doctorId, DateOnly date);
    Task<bool> DoctorSlotTakenAsync(string doctorId, DateOnly date, TimeOnly start);
    Task<bool> PatientOverlapAsync(string patientId, DateOnly date, TimeOnly start, TimeOnly end);
    Task<bool> PatientHasBookingWithDoctorAsync(string patientId, string doctorId, DateOnly date);
    Task<List<Appointment>> QueryAsync(AppointmentFilter filter);
    Task<int> CountAsync(AppointmentFilter filter);
    void Add(Appointment appointment);
    Task SaveAsync();
}

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _db;
    public AppointmentRepository(AppDbContext db) => _db = db;

    public Task<Appointment?> GetAsync(int id) =>
        _db.Appointments.Include(a => a.Token).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<HashSet<TimeOnly>> BookedStartsAsync(string doctorId, DateOnly date) =>
        (await _db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == date && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StartTime).ToListAsync()).ToHashSet();

    public Task<bool> DoctorSlotTakenAsync(string doctorId, DateOnly date, TimeOnly start) =>
        _db.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.AppointmentDate == date
                                       && a.StartTime == start && a.Status != AppointmentStatus.Cancelled);

    public Task<bool> PatientOverlapAsync(string patientId, DateOnly date, TimeOnly start, TimeOnly end) =>
        _db.Appointments.AnyAsync(a => a.PatientId == patientId && a.AppointmentDate == date
                                       && a.Status != AppointmentStatus.Cancelled
                                       && a.StartTime < end && start < a.EndTime);

    public Task<bool> PatientHasBookingWithDoctorAsync(string patientId, string doctorId, DateOnly date) =>
        _db.Appointments.AnyAsync(a => a.PatientId == patientId && a.DoctorId == doctorId
                                       && a.AppointmentDate == date && a.Status != AppointmentStatus.Cancelled);

    private IQueryable<Appointment> Apply(IQueryable<Appointment> q, AppointmentFilter f)
    {
        if (f.PatientId is not null) q = q.Where(a => a.PatientId == f.PatientId);
        if (f.DoctorId is not null) q = q.Where(a => a.DoctorId == f.DoctorId);
        if (f.From is { } fromDate) q = q.Where(a => a.AppointmentDate >= fromDate);
        if (f.To is { } toDate) q = q.Where(a => a.AppointmentDate <= toDate);
        if (f.Status is { } status) q = q.Where(a => a.Status == status);
        if (f.ExcludeCancelled) q = q.Where(a => a.Status != AppointmentStatus.Cancelled);
        return q;
    }

    public Task<List<Appointment>> QueryAsync(AppointmentFilter f)
    {
        var q = Apply(_db.Appointments.AsNoTracking().Include(a => a.Doctor).Include(a => a.Patient).Include(a => a.Token), f);
        q = f.Descending
            ? q.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
            : q.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime);
        return q.Take(f.Take).ToListAsync();
    }

    public Task<int> CountAsync(AppointmentFilter f) => Apply(_db.Appointments.AsNoTracking(), f).CountAsync();
    public void Add(Appointment appointment) => _db.Appointments.Add(appointment);
    public Task SaveAsync() => _db.SaveChangesAsync();
}
