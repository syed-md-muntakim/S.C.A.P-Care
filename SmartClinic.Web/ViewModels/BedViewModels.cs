using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class BedFilter
{
    public BedType? Type { get; set; }
    public BedStatus? Status { get; set; }
    public string? Ward { get; set; }
    public string? Q { get; set; }
}

public sealed record BedSummary(int TotalBeds, int AvailableBeds, int TotalCabins, int AvailableCabins, int Occupied, int Maintenance);

public class BedListItem
{
    public int Id { get; set; }
    public string BedNumber { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public BedType Type { get; set; }
    public decimal DailyRate { get; set; }
    public BedStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? PatientName { get; set; }
    public string? PatientUserName { get; set; }
    public DateTime? AdmittedAt { get; set; }
}

public class BedIndexViewModel
{
    public BedFilter Filter { get; set; } = new();
    public List<BedListItem> Items { get; set; } = new();
    public List<string> Wards { get; set; } = new();
    public BedSummary Summary { get; set; } = new(0, 0, 0, 0, 0, 0);
    public bool CanManage { get; set; }
}

public class BedFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(20), Display(Name = "Bed / cabin number")]
    public string BedNumber { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Ward { get; set; } = string.Empty;

    [Display(Name = "Type")] public BedType Type { get; set; } = BedType.Bed;

    [Range(0, 1000000), Display(Name = "Daily rate (BDT)")]
    public decimal DailyRate { get; set; }

    [StringLength(200)] public string? Notes { get; set; }
}

public class AssignBedViewModel
{
    public int BedId { get; set; }
    public string BedNumber { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public BedType Type { get; set; }

    [Required(ErrorMessage = "Select a patient."), Display(Name = "Patient")]
    public string? PatientId { get; set; }

    [StringLength(200)] public string? Notes { get; set; }

    public List<SelectListItem> Patients { get; set; } = new();
}
