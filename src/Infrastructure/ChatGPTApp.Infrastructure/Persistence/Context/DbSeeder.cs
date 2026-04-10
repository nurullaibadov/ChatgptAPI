using ChatGPTApp.Domain.Entities;
using ChatGPTApp.Domain.Enums;
using ChatGPTApp.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatGPTApp.Infrastructure.Persistence.Context;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            // Seed SuperAdmin
            if (!await context.Users.AnyAsync())
            {
                var superAdmin = new User
                {
                    FirstName = "Super",
                    LastName = "Admin",
                    Email = "superadmin@chatgptapp.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                    Role = UserRole.SuperAdmin,
                    IsActive = true,
                    IsEmailConfirmed = true
                };

                var admin = new User
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@chatgptapp.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    IsEmailConfirmed = true
                };

                await context.Users.AddRangeAsync(superAdmin, admin);
                await context.SaveChangesAsync();

                logger.LogInformation("Seed users created: superadmin@chatgptapp.com / Admin@123456");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }
}
