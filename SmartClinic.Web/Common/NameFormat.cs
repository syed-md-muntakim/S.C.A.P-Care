namespace SmartClinic.Web.Common;

public static class NameFormat
{
    /// <summary>"Abdur Rahman" -> "Dr. Abdur Rahman" (no double prefix).</summary>
    public static string Doctor(string name) =>
        name.StartsWith("Dr. ", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Dr ", StringComparison.OrdinalIgnoreCase)
            ? name : "Dr. " + name;
}
