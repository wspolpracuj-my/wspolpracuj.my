using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using wspolpracujmy.Models;
using BCrypt.Net;

namespace wspolpracujmy.Data
{
    /// <summary>
    /// Seeder przygotowujący przykładowe dane testowe dla środowiska developerskiego.
    /// </summary>
    public static class TestDataSeeder
    {
        /// <summary>
        /// Wypełnia bazę danych zestawem danych testowych.
        /// </summary>
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("TestDataSeeder");
            var context = services.GetRequiredService<AppDbContext>();

            // Always perform a lightweight data cleanup for known enum/string mismatches
            // (this fixes cases where other tables' enum values leaked into GroupRequests.status)
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"GroupRequests\" SET status = 'Pending' WHERE status NOT IN ('Pending','Accepted','Declined');");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to run GroupRequests.status cleanup; continuing.");
            }

            // Always ensure that the admin account exists (independent of full test seeding).
            try
            {
                var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Login == "admin");
                if (existingAdmin == null)
                {
                    var admin = new User
                    {
                        Name = "Admin",
                        Surname = "Systemu",
                        Role = Role.Admin,
                        Login = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin")
                    };
                    context.Users.Add(admin);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded default admin user (login: admin).");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to ensure admin user exists; continuing.");
            }

            // avoid seeding when data already present
            if (await context.Users.CountAsync() > 1)
            {
                logger.LogInformation("Test seeder skipped: Users already exist.");
                return;
            }

            await context.Database.OpenConnectionAsync();
            await using var tx = await context.Database.BeginTransactionAsync();

            // defer constraints so we can insert circular/related rows in one transaction
            await context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED;");

            try
            {
                // Users (companies + students) - use BCrypt hash for passwords
                var u1 = new User { Id = 1, Name = "Krystyna", Surname = "Innowacji", Role = Role.Company, Login = "firma1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("firma1") };
                var u2 = new User { Id = 2, Name = "Joanna", Surname = "Nieinnowacyjna", Role = Role.Company, Login = "firma2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("firma2") };
                var u3 = new User { Id = 3, Name = "Student", Surname = "One", Role = Role.Student, Login = "student1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("student1") };
                var u4 = new User { Id = 4, Name = "Student", Surname = "Two", Role = Role.Student, Login = "student2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("student2") };
                var u5 = new User { Id = 5, Name = "Student", Surname = "Three", Role = Role.Student, Login = "student3", PasswordHash = BCrypt.Net.BCrypt.HashPassword("student3") };
                // Additional test users: one student without a group, and one company without projects
                var u6 = new User { Id = 6, Name = "Student", Surname = "NoGroup", Role = Role.Student, Login = "student4", PasswordHash = BCrypt.Net.BCrypt.HashPassword("student4") };
                var u7 = new User { Id = 7, Name = "SoloCompany", Surname = "NoProject", Role = Role.Company, Login = "firma3", PasswordHash = BCrypt.Net.BCrypt.HashPassword("firma3") };
                await context.Users.AddRangeAsync(u1, u2, u3, u4, u5, u6, u7);

                // Companies
                var c1 = new Company { Id = 1, UserId = 1, CompanyName = "Firma Innowacji", ContactEmail = "contact@firma1.example", User = u1 };
                var c2 = new Company { Id = 2, UserId = 2, CompanyName = "TechLabs", ContactEmail = "hello@techlabs.example", User = u2 };
                // Company without any projects (to test edge cases)
                var c3 = new Company { Id = 3, UserId = 7, CompanyName = "SoloCompany", ContactEmail = "contact@solo.example", User = u7 };
                await context.Companies.AddRangeAsync(c1, c2, c3);

                // Meeting types
                var mtOnline = new MeetingType { Id = 1, Type = "Online" };
                var mtCompany = new MeetingType { Id = 2, Type = "Siedziba firmy" };
                var mtUniversity = new MeetingType { Id = 3, Type = "UZ" };
                await context.Meeting_types.AddRangeAsync(mtOnline, mtCompany, mtUniversity);

                // Tags are now handled by a dedicated TagsSeeder.
                // The TestDataSeeder will look up tags in the DB when creating ProjectTags.

                // Projects
                var p1 = new Project
                {
                    Id = 1,
                    CompanyId = 1,
                    Topic = "System wspierania współpracy firm–studenci",
                    Description = "Projekt dla studentów i firm",
                    CreatedAt = DateTime.UtcNow,
                    MaxNumberGroupMembers = 5,
                    MeetingTypeId = mtOnline.Id,
                    LanguageDoc = LanguageDoc.Polish,
                    Priority = Priority.P3,
                    Company = c1,
                    MeetingType = mtOnline
                };

                var p2 = new Project
                {
                    Id = 2,
                    CompanyId = 2,
                    Topic = "System wsparcia dla startupów",
                    Description = "Wsparcie dla zespołów startupowych",
                    CreatedAt = DateTime.UtcNow,
                    MaxNumberGroupMembers = 5,
                    MeetingTypeId = mtCompany.Id,
                    LanguageDoc = LanguageDoc.English,
                    Priority = Priority.P3,
                    Company = c2,
                    MeetingType = mtCompany
                };
                await context.Projects.AddRangeAsync(p1, p2);

                // Groups — create and save before students to avoid circular FK dependency
                var g1 = new Group { Id = 1, Name = "Grupa Innowacji", ProjectId = p1.Id, IsAccepted = GroupStatus.Accepted, Project = p1, Leader = null! };
                var g2 = new Group { Id = 2, Name = "Grupa TechLabs", ProjectId = p2.Id, IsAccepted = GroupStatus.Pending, Project = p2, Leader = null! };
                await context.Groups.AddRangeAsync(g1, g2);
                await context.SaveChangesAsync(); // persist groups so students can reference them

                // Students (Student.Id is independent from User.Id)
                var s1 = new Student { Id = 1, UserId = u3.Id, GroupId = g1.Id, Email = "student1@example.com", User = u3, Group = g1 };
                var s2 = new Student { Id = 2, UserId = u4.Id, GroupId = g2.Id, Email = "student2@example.com", User = u4, Group = g2 };
                var s3 = new Student { Id = 3, UserId = u5.Id, GroupId = g1.Id, Email = "student3@example.com", User = u5, Group = g1 };
                // Student record without a group (GroupId = null)
                var s4 = new Student { Id = 4, UserId = u6.Id, GroupId = null, Email = "student4@example.com", User = u6, Group = null };
                await context.Students.AddRangeAsync(s1, s2, s3, s4);
                await context.SaveChangesAsync();

                // ProjectTags (many-to-many)
                // Build ProjectTags by looking up tags by name so the dedicated TagsSeeder can control the tag set.
                var ptList = new System.Collections.Generic.List<ProjectTag>();

                var t_dotnet = await context.Tags.SingleOrDefaultAsync(t => t.Name == "dotnet");
                var t_backend = await context.Tags.SingleOrDefaultAsync(t => t.Name == "backend");
                var t_webapi = await context.Tags.SingleOrDefaultAsync(t => t.Name == "webapi");
                var t_database = await context.Tags.SingleOrDefaultAsync(t => t.Name == "database");
                var t_academic = await context.Tags.SingleOrDefaultAsync(t => t.Name == "academic");
                var t_react = await context.Tags.SingleOrDefaultAsync(t => t.Name == "react");
                var t_startup = await context.Tags.SingleOrDefaultAsync(t => t.Name == "startup");

                if (t_dotnet != null) ptList.Add(new ProjectTag { ProjectId = p1.Id, TagId = t_dotnet.Id, Project = p1, Tag = t_dotnet });
                if (t_backend != null) ptList.Add(new ProjectTag { ProjectId = p1.Id, TagId = t_backend.Id, Project = p1, Tag = t_backend });
                if (t_webapi != null) ptList.Add(new ProjectTag { ProjectId = p1.Id, TagId = t_webapi.Id, Project = p1, Tag = t_webapi });
                if (t_database != null) ptList.Add(new ProjectTag { ProjectId = p1.Id, TagId = t_database.Id, Project = p1, Tag = t_database });
                if (t_academic != null) ptList.Add(new ProjectTag { ProjectId = p1.Id, TagId = t_academic.Id, Project = p1, Tag = t_academic });

                if (t_dotnet != null) ptList.Add(new ProjectTag { ProjectId = p2.Id, TagId = t_dotnet.Id, Project = p2, Tag = t_dotnet });
                if (t_react != null) ptList.Add(new ProjectTag { ProjectId = p2.Id, TagId = t_react.Id, Project = p2, Tag = t_react });
                if (t_backend != null) ptList.Add(new ProjectTag { ProjectId = p2.Id, TagId = t_backend.Id, Project = p2, Tag = t_backend });
                if (t_startup != null) ptList.Add(new ProjectTag { ProjectId = p2.Id, TagId = t_startup.Id, Project = p2, Tag = t_startup });

                if (ptList.Count > 0)
                {
                    await context.ProjectTags.AddRangeAsync(ptList);
                }

                // Comments
                var cA = new Comment { Id = 1, UserId = u3.Id, ProjectId = p1.Id, Content = "Fajny projekt, chętnie pomogę.", CreatedAt = DateTime.UtcNow, User = u3, Project = p1 };
                var cB = new Comment { Id = 2, UserId = u4.Id, ProjectId = p1.Id, Content = "Czy praca może być zdalna?", CreatedAt = DateTime.UtcNow, User = u4, Project = p1 };
                var cC = new Comment { Id = 3, UserId = u2.Id, ProjectId = p2.Id, Content = "Czy projekt jest już zatwierdzony?", CreatedAt = DateTime.UtcNow, User = u2, Project = p2 };
                await context.Comments.AddRangeAsync(cA, cB, cC);

                // Responses
                var r1 = new Response { Id = 1, CommentId = cA.Id, UserId = u1.Id, Content = "Dziękujemy — odpowiemy wkrótce.", CreatedAt = DateTime.UtcNow, Comment = cA, User = u1 };
                var r2 = new Response { Id = 2, CommentId = cC.Id, UserId = u2.Id, Content = "Potwierdzamy, projekt zatwierdzony.", CreatedAt = DateTime.UtcNow, Comment = cC, User = u2 };
                await context.Responses.AddRangeAsync(r1, r2);

                // Notifications
                var n1 = new Notification { Id = 1, UserId = u5.Id, Content = "Twoja prośba o dołączenie została zaakceptowana.", Status = NotificationStatus.NotRead, User = u5, CreatedAt = DateTime.UtcNow, GroupRequestId = null };
                var n2 = new Notification { Id = 2, UserId = u1.Id, Content = "Nowy komentarz do Twojego projektu.", Status = NotificationStatus.NotRead, User = u1, CreatedAt = DateTime.UtcNow, GroupRequestId = null };
                await context.Notifications.AddRangeAsync(n1, n2);

                // fix group leaders (assign Student objects as leaders) and update groups
                g1.Leader = s1;
                g1.LeaderId = s1.Id;
                g2.Leader = s2;
                g2.LeaderId = s2.Id;
                context.Groups.Update(g1);
                context.Groups.Update(g2);
                await context.SaveChangesAsync();

                // GroupRequests table does not have a model in the project — insert via raw SQL if table exists
                var now = DateTime.UtcNow;
                // Seed a sample GroupRequest via EF so enum mapping is respected
                var gr1 = new GroupRequest
                {
                    Id = 1,
                    GroupId = g1.Id,
                    ProjectId = g1.ProjectId,
                    StudentId = s3.Id,
                    CreatedByUserId = s3.UserId,
                    Group = g1,
                    Project = p1,
                    Student = s3,
                    CreatedByUser = u5,
                    Status = GroupStatus.Accepted,
                    Type = "join_request",
                    CreatedAt = now,
                    RespondedAt = now
                };
                await context.GroupRequests.AddAsync(gr1);

                await tx.CommitAsync();

                // Ensure PostgreSQL sequences are advanced past the seeded max ids
                // so subsequent inserts (without explicit Id) won't conflict.
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Users\"','id'), COALESCE((SELECT MAX(id) FROM \"Users\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Companies\"','id'), COALESCE((SELECT MAX(id) FROM \"Companies\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Meeting_types\"','id'), COALESCE((SELECT MAX(id) FROM \"Meeting_types\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Tags\"','id'), COALESCE((SELECT MAX(id) FROM \"Tags\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Project\"','id'), COALESCE((SELECT MAX(id) FROM \"Project\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Groups\"','id'), COALESCE((SELECT MAX(id) FROM \"Groups\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Students\"','id'), COALESCE((SELECT MAX(id) FROM \"Students\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Comments\"','id'), COALESCE((SELECT MAX(id) FROM \"Comments\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Responses\"','id'), COALESCE((SELECT MAX(id) FROM \"Responses\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Notifications\"','id'), COALESCE((SELECT MAX(id) FROM \"Notifications\"),0) + 1);");
                    await context.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"GroupRequests\"','id'), COALESCE((SELECT MAX(id) FROM \"GroupRequests\"),0) + 1);");
                }
                catch (DbException ex)
                {
                    // Non-fatal: log and continue. Sequence adjustment best-effort.
                    logger.LogWarning(ex, "Failed to adjust sequences after seeding; continuing.");
                }

                logger.LogInformation("Test data seeded.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while seeding test data, rolling back.");
                await tx.RollbackAsync();
                throw;
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }
}
