using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentFlow.Infrastructure.Identity;

namespace RentFlow.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<RentFlowDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await dbContext.Database.MigrateAsync();

        await EnsureRolesAsync(roleManager);
        await EnsureAdminAsync(userManager, roleManager);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Owner", "Tenant" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{role}'.");
                }
            }
        }
    }

    private static async Task EnsureAdminAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var adminEmail = "admin@rentflow.local";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(existingAdmin, "Admin");
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Failed to assign Admin role to existing admin user.");
                }
            }

            return;
        }

        var adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            AccountStatus = AccountStatus.Active,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, "Admin@12345");
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add Admin role: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
        }
    }
}