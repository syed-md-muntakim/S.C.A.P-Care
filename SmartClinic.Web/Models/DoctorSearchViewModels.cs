namespace SmartClinic.Web.Models;

public class DoctorSearchQuery
{
    public string? Keyword { get; set; }
    public string? Specialty { get; set; }
    public string? Sort { get; set; }          // name (default) | name_desc | newest
    public int Page { get; set; } = 1;
    // Hooks for later: public int? MinRating (Sprint 4 #15), public DateOnly? AvailableOn (Sprint 1 #1)
}

public class DoctorListItem
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public DateTime MemberSince { get; set; }

    public string DisplayName => FullName.StartsWith("Dr. ") || FullName.StartsWith("Dr ") ? FullName : "Dr. " + FullName;
    public string Initials => string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                                    .Where(p => !p.StartsWith("Dr", StringComparison.OrdinalIgnoreCase) || p.Length > 3)
                                                    .Take(2).Select(p => char.ToUpper(p[0])));
}

public class DoctorSearchResult
{
    public DoctorSearchQuery Query { get; set; } = new();
    public List<DoctorListItem> Items { get; set; } = new();
    public List<string> Specialties { get; set; } = new();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
