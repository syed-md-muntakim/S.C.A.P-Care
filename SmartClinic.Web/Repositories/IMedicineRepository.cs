using SmartClinic.Web.Models;

namespace SmartClinic.Web.Repositories;

public interface IMedicineRepository
{
    Task<List<Medicine>> GetAllAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<Medicine?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<MedicineStockMovement>> GetMovementsAsync(
        int medicineId,
        CancellationToken cancellationToken = default);

    void Add(Medicine medicine);

    void AddMovement(MedicineStockMovement movement);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
