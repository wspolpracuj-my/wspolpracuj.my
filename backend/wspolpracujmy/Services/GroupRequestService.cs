using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za tworzenie GroupRequest z mechanizmem sprzątania starych
    /// prośb i powiadomień (atomowo, w transakcji).
    /// </summary>
    public class GroupRequestService
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notificationService;

        public GroupRequestService(AppDbContext db, NotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Tworzy nową prośbę typu ProjectRequest dla grupy, jednocześnie oznaczając
        /// wszystkie poprzednie oczekujące prośby tego typu dla tej grupy jako
        /// Cancelled i usuwając powiązane powiadomienia.
        /// Wszystko wykonywane w jednej transakcji.
        /// </summary>
        /// <param name="groupId">Id grupy</param>
        /// <param name="projectId">Id projektu</param>
        /// <param name="createdByUserId">Id użytkownika tworzącego prośbę</param>
        /// <returns>Utworzona prośba lub wyjątek w przypadku błędu</returns>
        public async Task<GroupRequest> CreateProjectRequestWithCleanup(int groupId, int projectId, int createdByUserId)
        {
            // Load group and project with company info
            var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId)
                ?? throw new InvalidOperationException($"Grupa o id {groupId} nie została znaleziona.");

            var project = await _db.Projects.Include(p => p.Company).ThenInclude(c => c.User).FirstOrDefaultAsync(p => p.Id == projectId)
                ?? throw new InvalidOperationException($"Projekt o id {projectId} nie został znaleziony.");

            // Begin transaction for atomicity
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // A. Find old pending ProjectRequest entries for this group
                var oldRequests = await _db.GroupRequests
                    .Where(gr => gr.GroupId == groupId && gr.Type != null && gr.Type.ToLower() == "projectrequest" && gr.Status == GroupStatus.Pending)
                    .Include(gr => gr.Project)
                    .ToListAsync();

                if (oldRequests.Any())
                {
                    // B. Mark them as Cancelled
                    foreach (var old in oldRequests)
                    {
                        old.Status = GroupStatus.Cancelled;
                    }
                    _db.GroupRequests.UpdateRange(oldRequests);

                    // C. Remove related notifications.
                    // Best-effort strategy:
                    // Prefer deleting notifications that reference the GroupRequest FK.
                    var oldReqIds = oldRequests.Select(r => r.Id).ToList();

                    var deleteByFk = _db.Notifications.Where(n => n.GroupRequestId != null && oldReqIds.Contains(n.GroupRequestId.Value));
                    if (await deleteByFk.AnyAsync())
                    {
                        await deleteByFk.ExecuteDeleteAsync();
                    }
                    else
                    {
                        // Fallback: Attempt delete by LinkTarget containing request id
                        var possibleLinkTargets = oldReqIds.Select(id => $"/requests/{id}").ToList();
                        var deleteByLink = _db.Notifications.Where(n => n.LinkTarget != null && possibleLinkTargets.Contains(n.LinkTarget));
                        if (await deleteByLink.AnyAsync())
                        {
                            await deleteByLink.ExecuteDeleteAsync();
                        }
                        else
                        {
                            // Final fallback: delete notifications using exact content pattern used when creating project request
                            var contentsToRemove = oldRequests
                                .Where(r => r.Project != null)
                                .Select(r => $"Grupa {group.Name} wysłała prośbę o realizację Twojego projektu: {r.Project!.Topic}")
                                .Distinct()
                                .ToList();

                            if (contentsToRemove.Any())
                            {
                                var deleteByContent = _db.Notifications.Where(n => contentsToRemove.Contains(n.Content));
                                await deleteByContent.ExecuteDeleteAsync();
                            }
                        }
                    }
                }

                // D. Create new request and notify company
                var entity = new GroupRequest
                {
                    GroupId = groupId,
                    ProjectId = projectId,
                    StudentId = null,
                    CreatedByUserId = createdByUserId,
                    Type = "ProjectRequest",
                    CreatedAt = DateTime.UtcNow,
                    Status = GroupStatus.Pending,
                    Group = group,
                    Project = project
                };

                _db.GroupRequests.Add(entity);

                // Notify company user if available
                if (project.Company != null)
                {
                    var companyUser = await _db.Users.FindAsync(project.Company.UserId);
                    if (companyUser != null)
                    {
                        // Create notification after the request is persisted so we can set GroupRequestId
                        // We create the GroupRequest first, then call NotificationService below.
                    }
                }

                await _db.SaveChangesAsync();

                // After SaveChanges, entity.Id exists. Create notification via NotificationService so it stores GroupRequestId.
                if (project.Company != null)
                {
                    var companyUser = await _db.Users.FindAsync(project.Company.UserId);
                    if (companyUser != null)
                    {
                        var content = $"Grupa {group.Name} wysłała prośbę o realizację Twojego projektu: {project.Topic}";
                        await _notificationService.CreateNotificationAsync(companyUser.Id, content, linkTarget: $"/requests/{entity.Id}", groupRequestId: entity.Id);
                    }
                }

                await tx.CommitAsync();
                return entity;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
