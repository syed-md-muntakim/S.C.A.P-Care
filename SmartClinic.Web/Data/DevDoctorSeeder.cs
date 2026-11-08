using Microsoft.AspNetCore.Identity;
using SmartClinic.Web.Common;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

/// <summary>Development only: sample approved doctors so search/filter/paging can be tested. Password: Doctor@123</summary>
public static class DevDoctorSeeder
{
    private static readonly (string UserId, string Name, string Specialty)[] Samples =
    {
        ("dr.rahman", "Anisur Rahman", "Cardiology"), ("dr.zahid", "Zahid Hasan", "Cardiology"),
        ("dr.farhana", "Farhana Akter", "Dermatology"), ("dr.mahbub", "Mahbub Alam", "Neurology"),
        ("dr.nusrat", "Nusrat Jahan", "Gynecology"), ("dr.tanvir", "Tanvir Hasan", "Orthopedics"),
        ("dr.sadia", "Sadia Islam", "Pediatrics"), ("dr.kamal", "Kamal Hossain", "General Physician"),
        ("dr.rafiq", "Rafiq Ahmed", "ENT"), ("dr.shirin", "Shirin Sultana", "Ophthalmology"),
        ("dr.imran", "Imran Chowdhury", "Psychiatry"), ("dr.maliha", "Maliha Karim", "Dentistry")
    };

    public static async Task SeedAsync(IServiceProvider sp)
    {
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (userId, name, specialty) in Samples)
        {
            if (await users.FindByNameAsync(userId) is not null) continue;
            var u = new ApplicationUser
            {
                UserName = userId, Email = $"{userId}@smartclinic.local", EmailConfirmed = true,
                PhoneNumber = "01700000000", FullName = name, Specialty = specialty, IsApproved = true
            };
            var result = await users.CreateAsync(u, "Doctor@123");
            if (result.Succeeded) await users.AddToRoleAsync(u, Roles.Doctor);
        }
    }
}
