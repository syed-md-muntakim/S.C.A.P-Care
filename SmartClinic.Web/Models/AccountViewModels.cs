using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models;

public class LoginViewModel
{
    [Required, Display(Name = "User ID")] public string UserId { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, Display(Name = "Account type")] public string Role { get; set; } = "Patient";
    [Required, StringLength(30, MinimumLength = 3), Display(Name = "User ID")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use letters, digits, '.', '-' or '_' only.")]
    public string UserId { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Full name")] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, Phone, Display(Name = "Phone number")] public string PhoneNumber { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
