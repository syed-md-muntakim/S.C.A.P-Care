using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models.Foundation;

public class PatientAccountViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public DateTime MemberSince { get; set; }
    public bool IsActive { get; set; }
}

public class EditPatientAccountViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = string.Empty;
    [Required, Phone, Display(Name = "Phone number")] public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Date of birth")] public DateOnly? DateOfBirth { get; set; }
    [StringLength(20)] public string? Gender { get; set; }
    [StringLength(5), Display(Name = "Blood group")] public string? BloodGroup { get; set; }
    [StringLength(300)] public string? Address { get; set; }

    [StringLength(100), Display(Name = "Emergency contact name")] public string? EmergencyContactName { get; set; }
    [Phone, StringLength(20), Display(Name = "Emergency contact phone")] public string? EmergencyContactPhone { get; set; }

    [StringLength(500)] public string? Allergies { get; set; }
    [StringLength(500), Display(Name = "Chronic conditions")] public string? ChronicConditions { get; set; }

    public static EditPatientAccountViewModel From(PatientAccountViewModel a) => new()
    {
        FullName = a.FullName, Email = a.Email ?? string.Empty, PhoneNumber = a.PhoneNumber ?? string.Empty,
        DateOfBirth = a.DateOfBirth, Gender = a.Gender, BloodGroup = a.BloodGroup, Address = a.Address,
        EmergencyContactName = a.EmergencyContactName, EmergencyContactPhone = a.EmergencyContactPhone,
        Allergies = a.Allergies, ChronicConditions = a.ChronicConditions
    };
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")] public string CurrentPassword { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "New password")] public string NewPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class DeactivateAccountViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Your password")] public string Password { get; set; } = string.Empty;
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please tick the box to confirm.")]
    public bool Confirm { get; set; }
}

public sealed record PatientListItem(string UserId, string UserName, string FullName, string? Email, string? Phone,
    string PatientCode, DateTime RegisteredAt, bool IsActive);
