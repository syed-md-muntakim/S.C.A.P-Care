using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.Models;

/// <summary>
/// A medicine or medicine batch held by the clinic.
/// Quantity changes should be made through the stock adjustment workflow.
/// </summary>
public class Medicine : BaseEntity
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string? GenericName { get; set; }

    [StringLength(80)]
    public string? Strength { get; set; }

    [Required, StringLength(60)]
    public string DosageForm { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Unit { get; set; } = "Tablet";

    [StringLength(80)]
    public string? BatchNumber { get; set; }

    [StringLength(150)]
    public string? SupplierName { get; set; }

    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal UnitPrice { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    // SQL Server updates this token on every write. It prevents simultaneous
    // stock adjustments from silently overwriting one another.
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<MedicineStockMovement> StockMovements { get; set; }
        = new List<MedicineStockMovement>();
}
