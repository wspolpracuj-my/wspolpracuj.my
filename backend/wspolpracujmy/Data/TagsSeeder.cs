using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using wspolpracujmy.Models;

namespace wspolpracujmy.Data
{
    /// <summary>
    /// Seeder odpowiedzialny za inicjalizację i aktualizację słownika tagów.
    /// </summary>
    public static class TagsSeeder
    {
        /// <summary>
        /// Dodaje brakujące tagi do bazy danych.
        /// </summary>
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("TagsSeeder");
            var context = services.GetRequiredService<AppDbContext>();

            try
            {
                // Replace existing tags with the requested set.
                var requested = new[] {
                    "IT",
                    "Proof of Concept",
                    "Badawcze",
                    "Analityka danych",
                    "UX/UI Design",
                    "Aplikacja webowa",
                    "Aplikacja mobilna",
                    "Backend",
                    "Frontend",
                    "Fullstack",
                    "Bazy danych",
                    "Sztuczna inteligencja",
                    "Machine Learning",
                    "Automatyzacja",
                    "IoT",
                    "Embedded Systems",
                    "Cyberbezpieczeństwo",
                    "Testowanie oprogramowania",
                    "DevOps",
                    "Cloud",
                    "Integracje API",
                    "E-commerce",
                    "System zarządzania",
                    "Dashboard / raportowanie",
                    "Edukacja",
                    "Medycyna / HealthTech",
                    "Finanse / FinTech",
                    "Logistyka",
                    "Produkcja / przemysł",
                    "Inne"
                };

                // Simply insert missing tags without deleting existing ones to avoid transaction hangs
                // Each tag name is unique, so duplicates will be skipped
                foreach (var name in requested)
                {
                    var found = await context.Tags.SingleOrDefaultAsync(t => t.Name == name);
                    if (found == null)
                    {
                        await context.Tags.AddAsync(new Tag { Name = name });
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Tags seeded/updated ({Count}).", requested.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while seeding tags.");
                throw;
            }
        }
    }
}
