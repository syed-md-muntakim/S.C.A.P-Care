using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Common;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Services;

public interface IDoctorSearchService
{
    Task<DoctorSearchResult> SearchAsync(DoctorSearchQuery query);
    Task<DoctorListItem?> GetAsync(string id);
}

public class DoctorSearchService : IDoctorSearchService
{
    public const int PageSize = 9;
    private readonly AppDbContext _db;
    public DoctorSearchService(AppDbContext db) => _db = db;

    // Only approved + active users that hold the Doctor role.
    private IQueryable<ApplicationUser> ApprovedDoctors()
    {
        var doctorIds = from ur in _db.UserRoles
                        join r in _db.Roles on ur.RoleId equals r.Id
                        where r.Name == Roles.Doctor
                        select ur.UserId;
        return _db.Users.AsNoTracking()
                  .Where(u => u.IsApproved && u.IsActive && doctorIds.Contains(u.Id));
    }

    public async Task<DoctorSearchResult> SearchAsync(DoctorSearchQuery q)
    {
        var doctors = ApprovedDoctors();

        var specialties = await doctors.Where(d => d.Specialty != null)
                                       .Select(d => d.Specialty ?? "")
                                       .Distinct().OrderBy(s => s).ToListAsync();

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var kw = q.Keyword.Trim();
            doctors = doctors.Where(d => d.FullName.Contains(kw)
                                      || (d.UserName != null && d.UserName.Contains(kw))
                                      || (d.Specialty != null && d.Specialty.Contains(kw)));
        }
        if (!string.IsNullOrWhiteSpace(q.Specialty))
            doctors = doctors.Where(d => d.Specialty == q.Specialty);

        // TODO (after merges): filter by availability using Sprint 1 #1 schedule data,
        // and by rating / sort by rating using Sprint 4 #15 feedback data.

        doctors = q.Sort switch
        {
            "name_desc" => doctors.OrderByDescending(d => d.FullName),
            "newest" => doctors.OrderByDescending(d => d.CreatedAt),
            _ => doctors.OrderBy(d => d.FullName)
        };

        var total = await doctors.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        var page = Math.Clamp(q.Page, 1, totalPages);

        var items = await doctors.Skip((page - 1) * PageSize).Take(PageSize)
            .Select(d => new DoctorListItem { Id = d.Id, FullName = d.FullName, Specialty = d.Specialty, MemberSince = d.CreatedAt })
            .ToListAsync();

        return new DoctorSearchResult
        {
            Query = q, Items = items, Specialties = specialties,
            Page = page, TotalPages = totalPages, TotalCount = total
        };
    }

    public Task<DoctorListItem?> GetAsync(string id) =>
        ApprovedDoctors().Where(d => d.Id == id)
            .Select(d => new DoctorListItem { Id = d.Id, FullName = d.FullName, Specialty = d.Specialty, MemberSince = d.CreatedAt })
            .FirstOrDefaultAsync();
}
