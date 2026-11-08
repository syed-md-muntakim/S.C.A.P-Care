using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Services;

public interface IMedicineInventoryService
{
    Task<MedicineInventoryViewModel> GetInventoryAsync(
        string? searchTerm,
        string? statusFilter);

    Task<MedicineInventorySummaryViewModel> GetSummaryAsync();

    Task<MedicineFormViewModel?> GetFormAsync(int id);

    Task<MedicineDetailsViewModel?> GetDetailsAsync(int id);

    Task CreateAsync(MedicineFormViewModel model, string performedBy);

    Task<MedicineWriteResult> UpdateAsync(int id, MedicineFormViewModel model);

    Task<StockAdjustmentResult> AdjustStockAsync(
        MedicineStockAdjustmentViewModel model,
        string performedBy);

    Task<MedicineWriteResult> DeleteAsync(int id);
}
