using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using wspolpracujmy.Models;
using BCrypt.Net;

namespace wspolpracujmy.Data
{
    public static class TestDataSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("TestDataSeeder");
            var context = services.GetRequiredService<AppDbContext>();

            // avoid seeding when data already present
            if (await context.Users.AnyAsync())
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
                await context.Users.AddRangeAsync(u1, u2, u3, u4, u5);

                // Companies
                var c1 = new Company { Id = 1, UserId = 1, CompanyName = "Firma Innowacji", ContactEmail = "contact@firma1.example", User = u1 };
                var c2 = new Company { Id = 2, UserId = 2, CompanyName = "TechLabs", ContactEmail = "hello@techlabs.example", User = u2 };
                await context.Companies.AddRangeAsync(c1, c2);

                // Meeting types
                var mtOnline = new MeetingType { Id = 1, Type = "online" };
                var mtOffline = new MeetingType { Id = 2, Type = "offline" };
                await context.Meeting_types.AddRangeAsync(mtOnline, mtOffline);

                // Tags
                var tags = new[] {
                    new Tag { Id = 1, Name = "dotnet" },
                    new Tag { Id = 2, Name = "react" },
                    new Tag { Id = 3, Name = "backend" },
                    new Tag { Id = 4, Name = "webapi" },
                    new Tag { Id = 5, Name = "database" },
                    new Tag { Id = 6, Name = "startup" },
                    new Tag { Id = 7, Name = "academic" }
                };
                await context.Tags.AddRangeAsync(tags);

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
                    MeetingTypeId = mtOffline.Id,
                    LanguageDoc = LanguageDoc.English,
                    Priority = Priority.P3,
                    Company = c2,
                    MeetingType = mtOffline
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
                await context.Students.AddRangeAsync(s1, s2, s3);
                await context.SaveChangesAsync();

                // ProjectTags (many-to-many)
                var pt = new[] {
                    new ProjectTag { ProjectId = p1.Id, TagId = 1, Project = p1, Tag = tags.Single(t => t.Id == 1) }, // dotnet
                    new ProjectTag { ProjectId = p1.Id, TagId = 3, Project = p1, Tag = tags.Single(t => t.Id == 3) }, // backend
                    new ProjectTag { ProjectId = p1.Id, TagId = 4, Project = p1, Tag = tags.Single(t => t.Id == 4) }, // webapi
                    new ProjectTag { ProjectId = p1.Id, TagId = 5, Project = p1, Tag = tags.Single(t => t.Id == 5) }, // database
                    new ProjectTag { ProjectId = p1.Id, TagId = 7, Project = p1, Tag = tags.Single(t => t.Id == 7) }, // academic

                    new ProjectTag { ProjectId = p2.Id, TagId = 1, Project = p2, Tag = tags.Single(t => t.Id == 1) }, // dotnet
                    new ProjectTag { ProjectId = p2.Id, TagId = 2, Project = p2, Tag = tags.Single(t => t.Id == 2) }, // react
                    new ProjectTag { ProjectId = p2.Id, TagId = 3, Project = p2, Tag = tags.Single(t => t.Id == 3) }, // backend
                    new ProjectTag { ProjectId = p2.Id, TagId = 6, Project = p2, Tag = tags.Single(t => t.Id == 6) }  // startup
                };
                await context.ProjectTags.AddRangeAsync(pt);

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
                var n1 = new Notification { Id = 1, UserId = u5.Id, Content = "Twoja prośba o dołączenie została zaakceptowana.", Status = NotificationStatus.NotRead, User = u5 };
                var n2 = new Notification { Id = 2, UserId = u1.Id, Content = "Nowy komentarz do Twojego projektu.", Status = NotificationStatus.NotRead, User = u1 };
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
                try
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO \"GroupRequests\" (\"id\", \"group_id\", \"student_id\", \"created_by_user_id\", \"status\", \"type\", \"created_at\", \"responded_at\") VALUES (1, 1, 3, 3, 'accepted', 'join_request', {0}, {0})",
                        parameters: new object[] { now });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "GroupRequests table not found or insert failed; skipping raw insert.");
                }

                await tx.CommitAsync();
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
