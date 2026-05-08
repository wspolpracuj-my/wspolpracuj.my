using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using wspolpracujmy.Models;

namespace wspolpracujmy.Data
{
    public static class TagsSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("TagsSeeder");
            var context = services.GetRequiredService<AppDbContext>();

            try
            {
                // Replace existing tags with the requested set.
                // Note: If there are FK constraints (ProjectTags) present, deletion may fail.
                // This is intended for development environments where the DB is fresh.

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

                await context.Database.OpenConnectionAsync();
                await using var tx = await context.Database.BeginTransactionAsync();
                await context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED;");

                // Try to remove existing tags. If this fails due to FK, log and continue by inserting missing tags.
                try
                {
                    var existing = await context.Tags.ToListAsync();
                    if (existing.Any())
                    {
                        context.Tags.RemoveRange(existing);
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not delete existing tags (FK constraints?). Will try to insert missing tags.");
                }

                // Insert requested tags if they don't already exist
                foreach (var name in requested)
                {
                    var found = await context.Tags.SingleOrDefaultAsync(t => t.Name == name);
                    if (found == null)
                    {
                        await context.Tags.AddAsync(new Tag { Name = name });
                    }
                }

                await context.SaveChangesAsync();
                await tx.CommitAsync();
                logger.LogInformation("Tags seeded/updated ({Count}).", requested.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while seeding tags.");
                throw;
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }
}
