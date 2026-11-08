using System.ComponentModel.DataAnnotations;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class MedicineInventorySummaryViewModel
{
    public int TotalMedicines { get; set; }
    public int LowStockMedicines { get; set; }
    public int OutOfStockMedicines { get; set; }
    public int ExpiringSoonMedicines { get; set; }
    public int ExpiredMedicines { get; set; }
}

public class MedicineInventoryViewModel : MedicineInventorySummaryViewModel
{
    public string? SearchTerm { get; set; }
    public string StatusFilter { get; set; } = "all";
    public List<MedicineInventoryItemViewModel> Medicines { get; set; } = new();
}

public class MedicineInventoryItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string DosageForm { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = "bg-secondary";
}

public class MedicineFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Medicine name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Generic name")]
    public string? GenericName { get; set; }

    [StringLength(80)]
    public string? Strength { get; set; }

    [Required, StringLength(60)]
    [Display(Name = "Dosage form")]
    public string DosageForm { get; set; } = string.Empty;

    [Required, StringLength(40)]
    [Display(Name = "Stock unit")]
    public string Unit { get; set; } = "Tablet";

    [StringLength(80)]
    [Display(Name = "Batch number")]
    public string? BatchNumber { get; set; }

    [StringLength(150)]
    [Display(Name = "Supplier")]
    public string? SupplierName { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Opening quantity")]
    public int QuantityInStock { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    [Display(Name = "Unit price")]
    public decimal UnitPrice { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expiry date")]
    public DateTime? ExpiryDate { get; set; }
}

public class MedicineStockAdjustmentViewModel
{
    public int MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public int CurrentQuantity { get; set; }

    [Display(Name = "Movement type")]
    [EnumDataType(typeof(StockMovementType))]
    public StockMovementType MovementType { get; set; } = StockMovementType.StockIn;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [StringLength(250)]
    [Display(Name = "Reason / notes")]
    public string? Notes { get; set; }
}

public class MedicineDetailsViewModel
{
    public MedicineFormViewModel Medicine { get; set; } = new();
    public List<MedicineStockMovement> StockMovements { get; set; } = new();
}

public enum MedicineWriteResult
{
    Success,
    NotFound,
    ConcurrencyConflict
}

public enum StockAdjustmentResult
{
    Success,
    NotFound,
    InsufficientStock,
    QuantityOverflow,
    InvalidMovementType,
    ConcurrencyConflict
}
