using Microsoft.AspNetCore.Identity;
using SmartClinic.Web.Common;
using SmartClinic.Web.Data.Foundation;
using SmartClinic.Web.Models;
using SmartClinic.Web.Models.Foundation;

namespace SmartClinic.Web.Services.Foundation;

public class FoundationDemoSeeder : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<FoundationDemoSeeder> _log;

    public FoundationDemoSeeder(IServiceProvider sp, IConfiguration cfg, ILogger<FoundationDemoSeeder> log)
    {
        _sp = sp; _cfg = cfg; _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_cfg.GetValue<bool>("Foundation:SeedDemoData")) return;
        try
        {
            using var scope = _sp.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var shifts = scope.ServiceProvider.GetRequiredService<IDoctorShiftRepository>();

            var rahman = await EnsureUserAsync(users, "dr.rahman", "Dr. Abdur Rahman", "dr.rahman@smartclinic.local", "01711000001", Roles.Doctor, "Cardiology", "Doctor@123");
            var sultana = await EnsureUserAsync(users, "dr.sultana", "Dr. Nusrat Sultana", "dr.sultana@smartclinic.local", "01711000002", Roles.Doctor, "Dermatology", "Doctor@123");
            await EnsureUserAsync(users, "patient1", "Demo Patient", "patient1@smartclinic.local", "01711000003", Roles.Patient, null, "Patient@123");

            foreach (var (doc, start, end, slot) in new[] { (rahman, new TimeOnly(10, 0), new TimeOnly(14, 0), 20), (sultana, new TimeOnly(16, 0), new TimeOnly(20, 0), 15) })
            {
                if (doc is null || (await shifts.ListByDoctorAsync(doc.Id)).Count > 0) continue;
                // Sunday..Thursday (Bangladesh working week)
                shifts.AddRange(Enumerable.Range(0, 5).Select(d => new DoctorShift
                {
                    DoctorId = doc.Id, DayOfWeek = (DayOfWeek)d, StartTime = start, EndTime = end,
                    SlotMinutes = slot, ValidFrom = ClinicTime.Today, IsActive = true
                }));
                await shifts.SaveAsync();
            }
            _log.LogInformation("Foundation demo data ready (dr.rahman / Doctor@123, dr.sultana / Doctor@123, patient1 / Patient@123).");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Foundation demo seeding skipped (is the database migrated?).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<ApplicationUser?> EnsureUserAsync(UserManager<ApplicationUser> users, string userName, string fullName,
        string email, string phone, string role, string? specialty, string password)
    {
        var existing = await users.FindByNameAsync(userName);
        if (existing is not null) return existing;

        var user = new ApplicationUser
        {
            UserName = userName, FullName = fullName, Email = email, PhoneNumber = phone, Specialty = specialty,
            EmailConfirmed = true, IsApproved = true, IsActive = true
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) return null;
        await users.AddToRoleAsync(user, role);
        return user;
    }
}
