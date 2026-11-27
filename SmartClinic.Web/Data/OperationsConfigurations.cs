using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> b)
    {
        b.ToTable("Beds");
        b.Property(x => x.DailyRate).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.BedNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.Type, x.Status });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BedAssignmentConfiguration : IEntityTypeConfiguration<BedAssignment>
{
    public void Configure(EntityTypeBuilder<BedAssignment> b)
    {
        b.ToTable("BedAssignments");
        b.HasOne(a => a.Bed).WithMany(x => x.Assignments).HasForeignKey(a => a.BedId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        // Database-level guarantees: one active patient per bed, one active bed per patient.
        b.HasIndex(a => a.BedId).IsUnique().HasFilter("[DischargedAt] IS NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_BedAssignments_OneActivePerBed");
        b.HasIndex(a => a.PatientId).IsUnique().HasFilter("[DischargedAt] IS NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_BedAssignments_OneActivePerPatient");
        b.HasQueryFilter(a => !a.IsDeleted);
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.ToTable("Appointments");
        b.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Doctor).WithMany().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        // Conflict detection at database level: a doctor slot can hold only one non-cancelled appointment.
        b.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.StartTime }).IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Status] <> 2")
            .HasDatabaseName("IX_Appointments_NoDoubleBooking");
        b.HasIndex(a => new { a.PatientId, a.AppointmentDate });
        b.HasIndex(a => new { a.DoctorId, a.AppointmentDate });
        b.HasQueryFilter(a => !a.IsDeleted);
    }
}

public class QueueTokenConfiguration : IEntityTypeConfiguration<QueueToken>
{
    public void Configure(EntityTypeBuilder<QueueToken> b)
    {
        b.ToTable("QueueTokens");
        b.HasOne(t => t.Appointment).WithOne(a => a.Token).HasForeignKey<QueueToken>(t => t.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Doctor).WithMany().HasForeignKey(t => t.DoctorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.Patient).WithMany().HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(t => new { t.DoctorId, t.QueueDate, t.TokenNumber }).IsUnique();
        // Only one patient can be "in consultation" per doctor per day.
        b.HasIndex(t => new { t.DoctorId, t.QueueDate }).IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [Status] = 1")
            .HasDatabaseName("IX_QueueTokens_OneInConsultation");
        b.HasQueryFilter(t => !t.IsDeleted);
    }
}
