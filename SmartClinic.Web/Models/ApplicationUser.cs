using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SmartClinic.Web.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [MaxLength(100)] public string? Specialty { get; set; }   // doctors only
    public bool IsApproved { get; set; } = true;               // doctors need admin approval
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
