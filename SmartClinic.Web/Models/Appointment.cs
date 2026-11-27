using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models;

// Numeric values are used in a database index filter (Cancelled = 2): do not renumber.
public enum AppointmentStatus { Booked = 0, Completed = 1, Cancelled = 2, NoShow = 3 }

public class Appointment : BaseEntity
{
    public string PatientId { get; set; } = string.Empty;
    public ApplicationUser? Patient { get; set; }

    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser? Doctor { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    [StringLength(300)] public string? Reason { get; set; }

    /// <summary>User id of whoever made the booking (the patient, or the admin at the front desk).</summary>
    public string? BookedById { get; set; }

    public QueueToken? Token { get; set; }
}
