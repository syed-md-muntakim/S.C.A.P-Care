using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineStockMovement> MedicineStockMovements => Set<MedicineStockMovement>();

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
