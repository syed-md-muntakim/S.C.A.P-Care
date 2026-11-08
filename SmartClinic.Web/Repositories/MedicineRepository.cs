using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Repositories;

public class MedicineRepository : IMedicineRepository
{
    private readonly AppDbContext _db;

    public MedicineRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Medicine>> GetAllAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Medicines
            .AsNoTracking()
            .Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            query = query.Where(m =>
                m.Name.Contains(term) ||
                (m.GenericName != null && m.GenericName.Contains(term)) ||
                (m.Strength != null && m.Strength.Contains(term)) ||
                (m.BatchNumber != null && m.BatchNumber.Contains(term)));
        }

        return await query
            .OrderBy(m => m.Name)
            .ThenBy(m => m.Strength)
            .ToListAsync(cancellationToken);
    }

    public Task<Medicine?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _db.Medicines.FirstOrDefaultAsync(
            m => m.Id == id && !m.IsDeleted,
            cancellationToken);
    }

    public Task<List<MedicineStockMovement>> GetMovementsAsync(
        int medicineId,
        CancellationToken cancellationToken = default)
    {
        return _db.MedicineStockMovements
            .AsNoTracking()
            .Where(m => m.MedicineId == medicineId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(Medicine medicine) => _db.Medicines.Add(medicine);

    public void AddMovement(MedicineStockMovement movement)
        => _db.MedicineStockMovements.Add(movement);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
