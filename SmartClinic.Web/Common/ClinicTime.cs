using System.Globalization;

namespace SmartClinic.Web.Common;


public static class ClinicTime
{
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Asia/Dhaka", "Bangladesh Standard Time" }) // Linux id, Windows id
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Local;
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
    public static DateOnly Today => DateOnly.FromDateTime(Now);
    public static TimeOnly TimeNow => TimeOnly.FromDateTime(Now);
}

/// <summary>Culture-safe formatting helpers (so ":" and "-" never change with server culture).</summary>
public static class DateTimeFormatExtensions
{
    public static string Hm(this TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);
    public static string Iso(this DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string Pretty(this DateOnly d) => d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
