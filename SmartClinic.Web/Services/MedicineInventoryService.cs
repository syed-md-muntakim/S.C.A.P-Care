using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;
using SmartClinic.Web.Repositories;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Services;

public class MedicineInventoryService : IMedicineInventoryService
{
    private const int ExpiringSoonDays = 30;
    private readonly IMedicineRepository _repository;

    public MedicineInventoryService(IMedicineRepository repository)
    {
        _repository = repository;
    }

    public async Task<MedicineInventoryViewModel> GetInventoryAsync(
        string? searchTerm,
        string? statusFilter)
    {
        var medicines = await _repository.GetAllAsync(searchTerm);
        var today = DateTime.Today;
        var normalizedFilter = (statusFilter ?? "all").Trim().ToLowerInvariant();

        var filteredMedicines = normalizedFilter switch
        {
            "low" => medicines.Where(m =>
                m.QuantityInStock > 0 &&
                m.QuantityInStock <= m.ReorderLevel),

            "out" => medicines.Where(m => m.QuantityInStock == 0),

            "expiring" => medicines.Where(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value.Date >= today &&
                m.ExpiryDate.Value.Date <= today.AddDays(ExpiringSoonDays)),

            "expired" => medicines.Where(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value.Date < today),

            "in-stock" => medicines.Where(m =>
                m.QuantityInStock > m.ReorderLevel),

            _ => medicines
        };

        var summary = BuildSummary(medicines, today);

        return new MedicineInventoryViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = normalizedFilter,
            TotalMedicines = summary.TotalMedicines,
            LowStockMedicines = summary.LowStockMedicines,
            OutOfStockMedicines = summary.OutOfStockMedicines,
            ExpiringSoonMedicines = summary.ExpiringSoonMedicines,
            ExpiredMedicines = summary.ExpiredMedicines,
            Medicines = filteredMedicines
                .Select(m => ToListItem(m, today))
                .ToList()
        };
    }

    public async Task<MedicineInventorySummaryViewModel> GetSummaryAsync()
    {
        var medicines = await _repository.GetAllAsync(null);
        return BuildSummary(medicines, DateTime.Today);
    }

    public async Task<MedicineFormViewModel?> GetFormAsync(int id)
    {
        var medicine = await _repository.GetByIdAsync(id);
        return medicine is null ? null : ToFormModel(medicine);
    }

    public async Task<MedicineDetailsViewModel?> GetDetailsAsync(int id)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine is null)
        {
            return null;
        }

        var movements = await _repository.GetMovementsAsync(id);

        return new MedicineDetailsViewModel
        {
            Medicine = ToFormModel(medicine),
            StockMovements = movements
        };
    }

    public async Task CreateAsync(MedicineFormViewModel model, string performedBy)
    {
        var medicine = new Medicine
        {
            Name = model.Name.Trim(),
            GenericName = CleanOptional(model.GenericName),
            Strength = CleanOptional(model.Strength),
            DosageForm = model.DosageForm.Trim(),
            Unit = model.Unit.Trim(),
            BatchNumber = CleanOptional(model.BatchNumber),
            SupplierName = CleanOptional(model.SupplierName),
            QuantityInStock = model.QuantityInStock,
            ReorderLevel = model.ReorderLevel,
            UnitPrice = model.UnitPrice,
            ExpiryDate = model.ExpiryDate?.Date
        };

        _repository.Add(medicine);

        if (medicine.QuantityInStock > 0)
        {
            _repository.AddMovement(new MedicineStockMovement
            {
                Medicine = medicine,
                MovementType = StockMovementType.StockIn,
                QuantityChanged = medicine.QuantityInStock,
                QuantityBefore = 0,
                QuantityAfter = medicine.QuantityInStock,
                Notes = "Opening stock",
                PerformedBy = NormalizePerformedBy(performedBy)
            });
        }

        await _repository.SaveChangesAsync();
    }

    public async Task<MedicineWriteResult> UpdateAsync(int id, MedicineFormViewModel model)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine is null)
        {
            return MedicineWriteResult.NotFound;
        }

        medicine.Name = model.Name.Trim();
        medicine.GenericName = CleanOptional(model.GenericName);
        medicine.Strength = CleanOptional(model.Strength);
        medicine.DosageForm = model.DosageForm.Trim();
        medicine.Unit = model.Unit.Trim();
        medicine.BatchNumber = CleanOptional(model.BatchNumber);
        medicine.SupplierName = CleanOptional(model.SupplierName);
        medicine.ReorderLevel = model.ReorderLevel;
        medicine.UnitPrice = model.UnitPrice;
        medicine.ExpiryDate = model.ExpiryDate?.Date;

        // Stock is intentionally not overwritten here. Use AdjustStockAsync
        // so every stock change is recorded in the movement history.
        try
        {
            await _repository.SaveChangesAsync();
            return MedicineWriteResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return MedicineWriteResult.ConcurrencyConflict;
        }
    }

    public async Task<StockAdjustmentResult> AdjustStockAsync(
        MedicineStockAdjustmentViewModel model,
        string performedBy)
    {
        var medicine = await _repository.GetByIdAsync(model.MedicineId);
        if (medicine is null)
        {
            return StockAdjustmentResult.NotFound;
        }

        if (!Enum.IsDefined(typeof(StockMovementType), model.MovementType))
        {
            return StockAdjustmentResult.InvalidMovementType;
        }

        if (model.Quantity < 1)
        {
            return StockAdjustmentResult.InsufficientStock;
        }

        var before = medicine.QuantityInStock;
        int after;

        if (model.MovementType == StockMovementType.StockOut)
        {
            if (model.Quantity > before)
            {
                return StockAdjustmentResult.InsufficientStock;
            }

            after = before - model.Quantity;
        }
        else
        {
            try
            {
                after = checked(before + model.Quantity);
            }
            catch (OverflowException)
            {
                return StockAdjustmentResult.QuantityOverflow;
            }
        }

        medicine.QuantityInStock = after;

        _repository.AddMovement(new MedicineStockMovement
        {
            MedicineId = medicine.Id,
            MovementType = model.MovementType,
            QuantityChanged = model.Quantity,
            QuantityBefore = before,
            QuantityAfter = after,
            Notes = CleanOptional(model.Notes),
            PerformedBy = NormalizePerformedBy(performedBy)
        });

        try
        {
            await _repository.SaveChangesAsync();
            return StockAdjustmentResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent stock change was saved after this request read the row.
            // The failed SaveChanges transaction also rolls back the movement insert.
            return StockAdjustmentResult.ConcurrencyConflict;
        }
    }

    public async Task<MedicineWriteResult> DeleteAsync(int id)
    {
        var medicine = await _repository.GetByIdAsync(id);
        if (medicine is null)
        {
            return MedicineWriteResult.NotFound;
        }

        // BaseEntity uses soft deletion so stock movement history is retained.
        medicine.IsDeleted = true;

        try
        {
            await _repository.SaveChangesAsync();
            return MedicineWriteResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return MedicineWriteResult.ConcurrencyConflict;
        }
    }

    private static MedicineInventorySummaryViewModel BuildSummary(
        IReadOnlyCollection<Medicine> medicines,
        DateTime today)
    {
        return new MedicineInventorySummaryViewModel
        {
            TotalMedicines = medicines.Count,
            LowStockMedicines = medicines.Count(m =>
                m.QuantityInStock > 0 &&
                m.QuantityInStock <= m.ReorderLevel),
            OutOfStockMedicines = medicines.Count(m => m.QuantityInStock == 0),
            ExpiringSoonMedicines = medicines.Count(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value.Date >= today &&
                m.ExpiryDate.Value.Date <= today.AddDays(ExpiringSoonDays)),
            ExpiredMedicines = medicines.Count(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value.Date < today)
        };
    }

    private static MedicineInventoryItemViewModel ToListItem(
        Medicine medicine,
        DateTime today)
    {
        string status;
        string cssClass;

        if (medicine.ExpiryDate.HasValue &&
            medicine.ExpiryDate.Value.Date < today)
        {
            status = "Expired";
            cssClass = "bg-danger";
        }
        else if (medicine.QuantityInStock == 0)
        {
            status = "Out of stock";
            cssClass = "bg-danger";
        }
        else if (medicine.QuantityInStock <= medicine.ReorderLevel)
        {
            status = "Low stock";
            cssClass = "bg-warning text-dark";
        }
        else if (medicine.ExpiryDate.HasValue &&
                 medicine.ExpiryDate.Value.Date <= today.AddDays(ExpiringSoonDays))
        {
            status = "Expiring soon";
            cssClass = "bg-info text-dark";
        }
        else
        {
            status = "In stock";
            cssClass = "bg-success";
        }

        return new MedicineInventoryItemViewModel
        {
            Id = medicine.Id,
            Name = medicine.Name,
            GenericName = medicine.GenericName,
            Strength = medicine.Strength,
            DosageForm = medicine.DosageForm,
            Unit = medicine.Unit,
            BatchNumber = medicine.BatchNumber,
            QuantityInStock = medicine.QuantityInStock,
            ReorderLevel = medicine.ReorderLevel,
            UnitPrice = medicine.UnitPrice,
            ExpiryDate = medicine.ExpiryDate,
            StockStatus = status,
            StatusCssClass = cssClass
        };
    }

    private static MedicineFormViewModel ToFormModel(Medicine medicine)
    {
        return new MedicineFormViewModel
        {
            Id = medicine.Id,
            Name = medicine.Name,
            GenericName = medicine.GenericName,
            Strength = medicine.Strength,
            DosageForm = medicine.DosageForm,
            Unit = medicine.Unit,
            BatchNumber = medicine.BatchNumber,
            SupplierName = medicine.SupplierName,
            QuantityInStock = medicine.QuantityInStock,
            ReorderLevel = medicine.ReorderLevel,
            UnitPrice = medicine.UnitPrice,
            ExpiryDate = medicine.ExpiryDate
        };
    }

    private static string? CleanOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizePerformedBy(string? performedBy)
    {
        var value = string.IsNullOrWhiteSpace(performedBy)
            ? "Unknown"
            : performedBy.Trim();

        return value.Length <= 256 ? value : value[..256];
    }
}
