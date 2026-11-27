using SmartClinic.Web.Models;
using SmartClinic.Web.Repositories;

namespace SmartClinic.Web.Data;

/// <summary>Development only: sample wards and cabins so the Bed &amp; Cabin pages can be tried straight away.</summary>
public static class DevBedSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var beds = sp.GetRequiredService<IBedRepository>();
        if (await beds.AnyAsync()) return;

        var list = new List<Bed>();
        for (var i = 1; i <= 6; i++) list.Add(new Bed { BedNumber = $"A-{100 + i}", Ward = "General Ward A", Type = BedType.Bed, DailyRate = 800 });
        for (var i = 1; i <= 4; i++) list.Add(new Bed { BedNumber = $"B-{200 + i}", Ward = "Ward B (Semi-private)", Type = BedType.Bed, DailyRate = 1500 });
        for (var i = 1; i <= 4; i++) list.Add(new Bed { BedNumber = $"C-{300 + i}", Ward = "Cabin Block", Type = BedType.Cabin, DailyRate = 3500, Notes = "Attached bathroom, TV" });
        list[9].Status = BedStatus.Maintenance; // one bed under maintenance for the demo

        beds.AddRange(list);
        await beds.SaveAsync();
    }
}
