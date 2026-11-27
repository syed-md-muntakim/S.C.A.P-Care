namespace SmartClinic.Web.Models;

// Numeric values are used in a database index filter (InConsultation = 1): do not renumber.
public enum TokenStatus { Waiting = 0, InConsultation = 1, Completed = 2, Skipped = 3 }

/// <summary>Queue ticket issued to a patient on the day of the appointment (check-in).</summary>
public class QueueToken : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser? Doctor { get; set; }

    public string PatientId { get; set; } = string.Empty;
    public ApplicationUser? Patient { get; set; }

    public DateOnly QueueDate { get; set; }
    public int TokenNumber { get; set; }
    public TokenStatus Status { get; set; } = TokenStatus.Waiting;

    public DateTime IssuedAt { get; set; }
    public DateTime? CalledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
