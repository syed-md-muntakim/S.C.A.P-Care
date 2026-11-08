using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Data.Foundation;



public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> b)
    {
        b.ToTable("PatientProfiles");
        b.HasIndex(p => p.UserId).IsUnique();
        b.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}

public class DoctorShiftConfiguration : IEntityTypeConfiguration<DoctorShift>
{
    public void Configure(EntityTypeBuilder<DoctorShift> b)
    {
        b.ToTable("DoctorShifts");
        b.HasIndex(s => new { s.DoctorId, s.DayOfWeek });
        b.HasOne(s => s.Doctor).WithMany().HasForeignKey(s => s.DoctorId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(s => !s.IsDeleted);
    }
}

public class DoctorLeaveConfiguration : IEntityTypeConfiguration<DoctorLeave>
{
    public void Configure(EntityTypeBuilder<DoctorLeave> b)
    {
        b.ToTable("DoctorLeaves");
        b.HasIndex(l => new { l.DoctorId, l.StartDate });
        b.HasOne(l => l.Doctor).WithMany().HasForeignKey(l => l.DoctorId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(l => !l.IsDeleted);
    }
}
