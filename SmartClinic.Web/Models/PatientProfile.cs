using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartClinic.Web.Models.Foundation;


public class PatientProfile : BaseEntity
{
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    [MaxLength(5)] public string? BloodGroup { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(100)] public string? EmergencyContactName { get; set; }
    [MaxLength(20)] public string? EmergencyContactPhone { get; set; }
    [MaxLength(500)] public string? Allergies { get; set; }
    [MaxLength(500)] public string? ChronicConditions { get; set; }


    [NotMapped] public string PatientCode => $"PT-{Id:D6}";
}
