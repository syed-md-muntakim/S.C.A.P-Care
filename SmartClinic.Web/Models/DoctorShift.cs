namespace SmartClinic.Web.Models.Foundation;

public class DoctorShift : BaseEntity
{
    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser? Doctor { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; } = 20;

    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }     
    public bool IsActive { get; set; } = true;  
}
