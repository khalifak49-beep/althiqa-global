using HomeMaids.Models;
using Microsoft.AspNetCore.Identity;

namespace HomeMaids.Data;

public static class DbInitializer
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";

    public static async Task SeedRolesAndAdminAsync(IServiceProvider sp, IConfiguration config)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in new[] { AdminRole, CustomerRole })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var section = config.GetSection("AdminSeed");
        var email = section["Email"] ?? "admin@homemaids.local";
        var password = section["Password"] ?? "Admin@123456";
        var fullName = section["FullName"] ?? "Administrator";

        var admin = await userManager.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AdminRole);
        }
        else if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }
    }
}
