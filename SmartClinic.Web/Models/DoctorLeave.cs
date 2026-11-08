using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models.Foundation;

public enum LeaveType { Casual, Sick, Annual, Conference, Emergency, Other }

public class DoctorLeave : BaseEntity
{
    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser? Doctor { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public LeaveType LeaveType { get; set; } = LeaveType.Casual;
    [MaxLength(200)] public string? Reason { get; set; }
}
