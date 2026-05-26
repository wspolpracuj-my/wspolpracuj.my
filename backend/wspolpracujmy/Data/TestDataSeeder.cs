using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using wspolpracujmy.Models;

namespace wspolpracujmy.Data
{
    /// <summary>
    /// Seeder danych testowych wykorzystywany przy starcie aplikacji.
    /// </summary>
    public static class TestDataSeeder
    {
        /// <summary>
        /// Tworzy podstawowe konto administratora, jeśli jeszcze nie istnieje.
        /// </summary>
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("TestDataSeeder");
            var context = services.GetRequiredService<AppDbContext>();

            try
            {
                const string adminLogin = "admin";
                const string adminPassword = "admin";

                var existingAdmin = await context.Users
                    .FirstOrDefaultAsync(u => u.Login == adminLogin);

                if (existingAdmin == null)
                {
                    var admin = new User
                    {
                        Name = "Admin",
                        Surname = "System",
                        Role = Role.Admin,
                        Login = adminLogin,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword)
                    };

                    await context.Users.AddAsync(admin);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Admin user seeded with login {Login}.", adminLogin);
                }
                else
                {
                    logger.LogInformation("Admin user already exists with login {Login}.", adminLogin);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while seeding test data.");
                throw;
            }
        }
    }
}
