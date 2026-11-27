using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public sealed record BookingSlot(string Start, string End, bool Booked);
public sealed record BookingDay(string Date, bool IsPast, bool OnLeave, string? LeaveReason, IReadOnlyList<BookingSlot> Slots);

public sealed record BookingRequest(string PatientId, string DoctorId, DateOnly Date, TimeOnly Start, string? Reason, string BookedById);

public class BookAppointmentViewModel
{
    [Required(ErrorMessage = "Select a doctor."), Display(Name = "Doctor")]
    public string DoctorId { get; set; } = string.Empty;

    /// <summary>Only used when an admin books on behalf of a patient.</summary>
    [Display(Name = "Patient")] public string? PatientId { get; set; }

    [Required(ErrorMessage = "Select a date.")] public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Choose a time slot.")] public TimeOnly? StartTime { get; set; }

    [StringLength(300), Display(Name = "Reason for visit (optional)")]
    public string? Reason { get; set; }

    public bool IsAdmin { get; set; }
    public string MinDate { get; set; } = string.Empty;
    public string MaxDate { get; set; } = string.Empty;
    public List<SelectListItem> Doctors { get; set; } = new();
    public List<SelectListItem> Patients { get; set; } = new();
}

public class AppointmentListQuery
{
    public string? Scope { get; set; }            // upcoming (default) | today | past | all
    public string? DoctorId { get; set; }         // admin filter
    public AppointmentStatus? Status { get; set; }
}

public class AppointmentListItem
{
    public int Id { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Reason { get; set; }
    public int? TokenNumber { get; set; }
    public TokenStatus? TokenStatus { get; set; }
    public bool CanGetToken { get; set; }
}

public class AppointmentIndexViewModel
{
    public string Role { get; set; } = string.Empty;
    public AppointmentListQuery Query { get; set; } = new();
    public List<AppointmentListItem> Items { get; set; } = new();
    public List<SelectListItem> Doctors { get; set; } = new();
}
