using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data.Foundation;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineStockMovement> MedicineStockMovements => Set<MedicineStockMovement>();

    // Sprint 1 Foundation (schedules + patient accounts)
    public DbSet<DoctorShift> DoctorShifts => Set<DoctorShift>();
    public DbSet<DoctorLeave> DoctorLeaves => Set<DoctorLeave>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();

    // Sprint 1 #20 Bed & Cabin, Sprint 2 #2 Appointment Booking, #4 Queue and Token
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<BedAssignment> BedAssignments => Set<BedAssignment>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<QueueToken> QueueTokens => Set<QueueToken>();

    // Other sprint entities will be added by their respective feature branches.

    // Sprint 1: DbSet<DoctorShift>, DbSet<Bed>, DbSet<Medicine> ...
    // Sprint 2: DbSet<Appointment>, DbSet<Token>, DbSet<Invoice> ...
    // Sprint 3: DbSet<VisitRecord>, DbSet<LabOrder>, DbSet<Payment> ...
    // Sprint 4: DbSet<Prescription>, DbSet<Feedback> ...

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().HasIndex(u => u.Specialty);
        
        builder.Entity<Medicine>()
            .HasIndex(m => m.Name);

        builder.Entity<Medicine>()
            .HasIndex(m => new { m.IsDeleted, m.ExpiryDate });

        builder.Entity<Medicine>()
            .Property(m => m.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<MedicineStockMovement>()
            .HasOne(m => m.Medicine)
            .WithMany(m => m.StockMovements)
            .HasForeignKey(m => m.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ApplyConfiguration(new PatientProfileConfiguration());
        builder.ApplyConfiguration(new DoctorShiftConfiguration());
        builder.ApplyConfiguration(new DoctorLeaveConfiguration());

        builder.ApplyConfiguration(new BedConfiguration());
        builder.ApplyConfiguration(new BedAssignmentConfiguration());
        builder.ApplyConfiguration(new AppointmentConfiguration());
        builder.ApplyConfiguration(new QueueTokenConfiguration());
    }

    public override int SaveChanges() { Stamp(); return base.SaveChanges(); }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Stamp();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void Stamp()
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added) e.Entity.CreatedAt = DateTime.UtcNow;
            else if (e.State == EntityState.Modified) e.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
