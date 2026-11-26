using Microsoft.AspNetCore.Identity;

namespace DemoMVC.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var roleName in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"creating role '{roleName}'");
            }
        }

        await EnsureUserAsync(userManager, "admin@gmail.com", "Admin@123", "Admin");
        await EnsureUserAsync(userManager, "user@gmail.com", "User@123", "User");
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            EnsureSucceeded(result, $"creating user '{email}'");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(result, $"adding user '{email}' to role '{role}'");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed while {operation}: {errors}");
        }
    }
}
