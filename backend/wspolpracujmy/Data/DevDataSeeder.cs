using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using wspolpracujmy.Models;

namespace wspolpracujmy.Data;

public static class DevDataSeeder
{
    public static async Task SeedIfDevelopmentAsync(this WebApplication app)
    {
        var env = app.Services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
            return;

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure the enum type exists in the public schema (pre-create to avoid type conflicts)
        var createEnumSql = @"DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'status_enum') THEN
                CREATE TYPE status_enum AS ENUM ('pending', 'accepted', 'rejected');
            END IF;
        END
        $$;";
        await context.Database.ExecuteSqlRawAsync(createEnumSql);

        // Ensure DB exists (for dev only)
        await context.Database.EnsureCreatedAsync();

        // Seed minimal sample data if empty
        if (await context.Services.AnyAsync())
            return;

        var svc1 = new Service { Name = "IT Consulting" };
        var svc2 = new Service { Name = "Marketing" };
        context.Services.AddRange(svc1, svc2);
        await context.SaveChangesAsync();

        var comp1 = new Company
        {
            Tin = "TIN001",
            Name = "Alpha Ltd",
            ServiceId = svc1.Id,
            OfferId = svc2.Id,
            ContactEmail = "contact@alpha.example",
            Website = "https://alpha.example",
            Location = "Warsaw"
        };

        var comp2 = new Company
        {
            Tin = "TIN002",
            Name = "Beta GmbH",
            ServiceId = svc2.Id,
            OfferId = svc1.Id,
            ContactEmail = "contact@beta.example",
            Website = "https://beta.example",
            Location = "Berlin"
        };

        context.Companies.AddRange(comp1, comp2);
        await context.SaveChangesAsync();

        var user1 = new User { Mail = "admin@alpha.example", Password = "password", CompanyTin = comp1.Tin, Verified = true };
        var user2 = new User { Mail = "admin@beta.example", Password = "password", CompanyTin = comp2.Tin, Verified = false };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var match = new Match { CompanyTin = comp1.Tin, MatchedCompanyTin = comp2.Tin, Status = StatusEnum.pending };
        context.Matches.Add(match);
        await context.SaveChangesAsync();
    }
}
