using System.ComponentModel.DataAnnotations;
using SmartClinic.Web.Common;

namespace SmartClinic.Web.Models.Foundation;


public sealed record TimeSlot(TimeOnly Start, TimeOnly End);
public sealed record DaySchedule(DateOnly Date, bool IsOnLeave, string? LeaveReason, IReadOnlyList<TimeSlot> Slots);

public sealed record ScheduleEvent(string Id, string Title, string Start, string? End, bool AllDay, string Color);


public class ShiftFormViewModel
{
    public int? Id { get; set; }                      
    [Required] public string DoctorId { get; set; } = string.Empty;
    public string? DoctorName { get; set; }        

    public List<int> Days { get; set; } = new();

    [Display(Name = "Start time")] public TimeOnly StartTime { get; set; } = new(9, 0);
    [Display(Name = "End time")] public TimeOnly EndTime { get; set; } = new(13, 0);
    [Range(5, 120), Display(Name = "Slot length (minutes)")] public int SlotMinutes { get; set; } = 20;
    [Display(Name = "Valid from")] public DateOnly ValidFrom { get; set; } = ClinicTime.Today;
    [Display(Name = "Valid until (optional)")] public DateOnly? ValidTo { get; set; }
}

public class LeaveFormViewModel
{
    [Required] public string DoctorId { get; set; } = string.Empty;
    public string? DoctorName { get; set; }

    [Display(Name = "From")] public DateOnly StartDate { get; set; } = ClinicTime.Today;
    [Display(Name = "To")] public DateOnly EndDate { get; set; } = ClinicTime.Today;
    [Display(Name = "Leave type")] public LeaveType LeaveType { get; set; } = LeaveType.Casual;
    [StringLength(200)] public string? Reason { get; set; }
}

public class ScheduleIndexViewModel
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public bool IsAdminView { get; set; }
    public IReadOnlyList<DoctorShift> Shifts { get; set; } = Array.Empty<DoctorShift>();
    public IReadOnlyList<DoctorLeave> Leaves { get; set; } = Array.Empty<DoctorLeave>();
}

public sealed record DoctorListItem(string UserId, string UserName, string FullName, string? Specialty,
    string? Email, string? Phone, bool IsApproved, bool IsActive, int ActiveShifts);
