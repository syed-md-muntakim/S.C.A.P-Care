using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models;

public enum BedType { Bed = 0, Cabin = 1 }
public enum BedStatus { Available = 0, Occupied = 1, Maintenance = 2 }

/// <summary>A ward bed or a private cabin. Status changes only through BedService.</summary>
public class Bed : BaseEntity
{
    [Required, StringLength(20)] public string BedNumber { get; set; } = string.Empty;
    [Required, StringLength(60)] public string Ward { get; set; } = string.Empty;
    public BedType Type { get; set; } = BedType.Bed;
    public decimal DailyRate { get; set; }
    public BedStatus Status { get; set; } = BedStatus.Available;
    [StringLength(200)] public string? Notes { get; set; }

    /// <summary>Optimistic concurrency: two admins cannot assign the same bed at once.</summary>
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<BedAssignment> Assignments { get; set; } = new List<BedAssignment>();
}

/// <summary>One admission to a bed. Active while DischargedAt is null.</summary>
public class BedAssignment : BaseEntity
{
    public int BedId { get; set; }
    public Bed? Bed { get; set; }

    public string PatientId { get; set; } = string.Empty;
    public ApplicationUser? Patient { get; set; }

    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    [StringLength(200)] public string? Notes { get; set; }
}
