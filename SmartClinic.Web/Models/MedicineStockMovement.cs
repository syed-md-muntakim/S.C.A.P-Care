using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models;

public enum StockMovementType
{
    StockIn = 1,
    StockOut = 2
}

/// <summary>
/// Immutable audit record for an inventory quantity change.
/// </summary>
public class MedicineStockMovement : BaseEntity
{
    public int MedicineId { get; set; }

    public Medicine Medicine { get; set; } = null!;

    public StockMovementType MovementType { get; set; }

    public int QuantityChanged { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    [StringLength(250)]
    public string? Notes { get; set; }

    [Required, StringLength(256)]
    public string PerformedBy { get; set; } = string.Empty;
}
