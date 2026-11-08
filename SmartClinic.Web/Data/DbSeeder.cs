using Microsoft.AspNetCore.Identity;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg)
    {
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var r in Roles.All)
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        var adminId = cfg["SeedAdmin:UserId"] ?? "admin";
        if (await users.FindByNameAsync(adminId) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminId,
                Email = cfg["SeedAdmin:Email"] ?? "admin@smartclinic.local",
                EmailConfirmed = true,
                FullName = "System Administrator",
                IsApproved = true
            };
            var result = await users.CreateAsync(admin, cfg["SeedAdmin:Password"] ?? "@dmin123");
            if (result.Succeeded) await users.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
