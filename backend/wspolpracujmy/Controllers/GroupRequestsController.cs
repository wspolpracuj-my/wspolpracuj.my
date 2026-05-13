using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;
using wspolpracujmy.Services;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do zarządzania prośbami grup (Invitation, ProjectRequest, Application).
    /// </summary>
    public class GroupRequestsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;
        private readonly GroupRequestService _groupRequestService;

        public GroupRequestsController(AppDbContext db, NotificationService notifications, GroupRequestService groupRequestService)
        {
            _db = db;
            _notifications = notifications;
            _groupRequestService = groupRequestService;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<GroupRequest>> Post([FromBody] CreateGroupRequestDto dto)
        {
            if (dto == null) return BadRequest();

            var group = await _db.Groups.Include(g => g.Project).Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == dto.GroupId);
            if (group == null) return NotFound($"Grupa o id {dto.GroupId} nie została znaleziona.");

            // current user
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var creatorUser = await _db.Users.FindAsync(currentUserId);
            if (creatorUser == null) return Unauthorized();
            var creatorStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == currentUserId);

            var reqType = (dto.Type ?? string.Empty).Trim();

            // Strict DTO validation & sanitization based on `Type`.
            var sanitizeResult = ValidateAndSanitizeDto(dto, reqType, group);
            if (!sanitizeResult.IsValid)
                return BadRequest(sanitizeResult.ErrorMessage);

            // business validations and determine target student when applicable
            int? targetStudentId = null;

            if (string.Equals(reqType, "Invitation", StringComparison.OrdinalIgnoreCase))
            {
                // Only leader can invite
                if (creatorStudent == null || !group.LeaderId.HasValue || creatorStudent.Id != group.LeaderId.Value)
                    return Forbid();

                var memberLimit = group.Project != null ? group.Project.MaxNumberGroupMembers : 5;
                var currentMembers = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                if (currentMembers >= memberLimit) return BadRequest($"Grupa ma już {currentMembers} członków, co przekracza limit {memberLimit}.");

                // Resolve target student strictly by email (client should not provide TargetStudentId)
                if (string.IsNullOrWhiteSpace(dto.TargetEmail))
                    return BadRequest("Dla typu Invitation pole TargetEmail jest wymagane; nie należy przekazywać TargetStudentId.");

                var email = dto.TargetEmail.Trim().ToLowerInvariant();
                var invitedByEmail = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Email.ToLower() == email);
                if (invitedByEmail == null) return NotFound($"Nie znaleziono studenta o adresie e-mail {dto.TargetEmail}.");
                var invited = invitedByEmail;
                if (invited.GroupId.HasValue) return BadRequest("Zaproszony student już należy do grupy.");
                targetStudentId = invited.Id;
            }
            else if (string.Equals(reqType, "ProjectRequest", StringComparison.OrdinalIgnoreCase))
            {
                if (creatorStudent == null || !group.LeaderId.HasValue || creatorStudent.Id != group.LeaderId.Value)
                    return Forbid();

                if (!dto.ProjectId.HasValue)
                    return BadRequest("Brak ProjectId dla typu ProjectRequest.");
                var projectId = dto.ProjectId.Value;

                var project = await _db.Projects.FindAsync(projectId);
                if (project == null) return NotFound($"Projekt o id {projectId} nie został znaleziony.");

                if (project.MaxGroups.HasValue)
                {
                    var acceptedCount = await _db.Groups.CountAsync(g => g.ProjectId == project.Id && g.IsAccepted == GroupStatus.Accepted);
                    if (acceptedCount >= project.MaxGroups.Value) return BadRequest($"Projekt osiągnął maksymalną liczbę grup ({project.MaxGroups.Value}).");
                }

                if (group.ProjectId.HasValue) return BadRequest("Grupa jest już przypisana do projektu.");

                targetStudentId = null;
            }
            else if (string.Equals(reqType, "Application", StringComparison.OrdinalIgnoreCase))
            {
                if (creatorStudent == null) return Forbid();
                if (creatorStudent.GroupId.HasValue) return BadRequest("Już należysz do grupy.");

                // For Application the target student is the group's leader.
                if (!group.LeaderId.HasValue) return BadRequest("Wybrana grupa nie ma lidera; nie można złożyć Application.");
                targetStudentId = group.LeaderId.Value;
            }
            else
            {
                return BadRequest("Nieobsługiwany typ prośby. Użyj 'Invitation', 'ProjectRequest' lub 'Application'.");
            }

            // load student entity if present
            Student? targetStudent = null;
            if (targetStudentId is int targetId)
                targetStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == targetId);

            // State-based validation: prevent duplicate or concurrent requests of same type for same relation
            var reqTypeNorm = reqType.ToLowerInvariant();
            if (reqTypeNorm == "projectrequest")
            {
                // same GroupId + ProjectId + Type
                var existing = await _db.GroupRequests
                    .Where(gr => gr.GroupId == dto.GroupId && gr.ProjectId == dto.ProjectId && gr.Type != null && gr.Type.ToLower() == reqTypeNorm)
                    .ToListAsync();

                if (existing.Any(e => e.Status == GroupStatus.Pending || e.Status == GroupStatus.Accepted))
                {
                    return BadRequest("Nie można wysłać prośby o ten projekt — istnieje już aktywna (oczekująca lub zaakceptowana) prośba dla tej grupy.");
                }
                // if all existing are Declined (or none exist) we allow creating a new one
                // For ProjectRequest we perform a cleanup + create in a single transactional operation
                if (!dto.ProjectId.HasValue) return BadRequest("Dla typu ProjectRequest pole ProjectId jest wymagane");
                var created = await _groupRequestService.CreateProjectRequestWithCleanup(dto.GroupId, dto.ProjectId.Value, currentUserId);
                return CreatedAtAction(nameof(Post), new { id = created.Id }, created);
            }
            else if (reqTypeNorm == "invitation" || reqTypeNorm == "invite")
            {
                if (!targetStudentId.HasValue)
                    return BadRequest("Target student not resolved for Invitation.");

                var existing = await _db.GroupRequests
                    .Where(gr => gr.GroupId == dto.GroupId && gr.StudentId == targetStudentId && gr.Type != null && gr.Type.ToLower() == reqTypeNorm)
                    .ToListAsync();

                if (existing.Any(e => e.Status == GroupStatus.Pending || e.Status == GroupStatus.Accepted))
                {
                    return BadRequest("Nie możesz wysłać zaproszenia — istnieje już aktywne (oczekujące lub zaakceptowane) zaproszenie dla tego studenta i tej grupy.");
                }
            }
            else if (reqTypeNorm == "application")
            {
                if (!targetStudentId.HasValue)
                    return BadRequest("Unable to resolve target student for Application.");

                var existing = await _db.GroupRequests
                    .Where(gr => gr.GroupId == dto.GroupId && gr.StudentId == targetStudentId && gr.Type != null && gr.Type.ToLower() == reqTypeNorm)
                    .ToListAsync();

                if (existing.Any(e => e.Status == GroupStatus.Pending || e.Status == GroupStatus.Accepted))
                {
                    return BadRequest("Nie możesz złożyć wniosku — istnieje już aktywna prośba o dołączenie dla tej relacji.");
                }
            }

            var entity = new GroupRequest
            {
                GroupId = dto.GroupId,
                ProjectId = dto.ProjectId,
                StudentId = targetStudentId,
                CreatedByUserId = currentUserId,
                Type = reqType,
                CreatedAt = DateTime.UtcNow,
                Status = GroupStatus.Pending,
                Group = group,
                Student = targetStudent,
                CreatedByUser = creatorUser
            };

            // transactionally insert GroupRequest and Notification(s)
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Re-check inside transaction to avoid races: ensure no Pending/Accepted exists anymore
                var reqTypeNormTx = reqType.ToLowerInvariant();
                if (reqTypeNormTx == "projectrequest")
                {
                    var existsTx = await _db.GroupRequests
                        .Where(gr => gr.GroupId == dto.GroupId && gr.ProjectId == dto.ProjectId && gr.Type != null && gr.Type.ToLower() == reqTypeNormTx)
                        .AnyAsync(e => e.Status == GroupStatus.Pending || e.Status == GroupStatus.Accepted);
                    if (existsTx) return BadRequest("Nie można wysłać prośby o ten projekt — istnieje już aktywna (oczekująca lub zaakceptowana) prośba dla tej grupy.");
                }
                else if ((reqTypeNormTx == "invitation" || reqTypeNormTx == "invite") && targetStudentId.HasValue)
                {
                    var existsTx = await _db.GroupRequests
                        .Where(gr => gr.GroupId == dto.GroupId && gr.StudentId == targetStudentId && gr.Type != null && gr.Type.ToLower() == reqTypeNormTx)
                        .AnyAsync(e => e.Status == GroupStatus.Pending || e.Status == GroupStatus.Accepted);
                    if (existsTx) return BadRequest("Nie możesz wysłać zaproszenia — istnieje już aktywne (oczekujące lub zaakceptowane) zaproszenie dla tego studenta i tej grupy.");
                }
                _db.GroupRequests.Add(entity);

                const string linkTarget = "/requests";

                // Collect notifications to create after the GroupRequest entity is saved
                var notificationsToCreate = new System.Collections.Generic.List<(int userId, string content, string? link)>();

                if (string.Equals(reqType, "Application", StringComparison.OrdinalIgnoreCase))
                {
                    // notify leader
                    if (group.LeaderId.HasValue)
                    {
                        var leaderId = group.LeaderId.Value;
                        var leader = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == leaderId);
                        if (leader != null)
                        {
                            var requesterName = creatorStudent?.User != null ? $"{creatorStudent.User.Name} {creatorStudent.User.Surname}" : creatorUser.Name + " " + creatorUser.Surname;
                            var content = $"Student {requesterName} chce dołączyć do Twojej grupy";
                            var leaderUser = await _db.Users.FindAsync(leader.UserId);
                            if (leaderUser != null)
                            {
                                notificationsToCreate.Add((leader.UserId, content, linkTarget));
                            }
                        }
                    }
                }
                else if (string.Equals(reqType, "Invitation", StringComparison.OrdinalIgnoreCase))
                {
                    if (targetStudent != null)
                    {
                        var leaderName = creatorStudent?.User != null ? $"{creatorStudent.User.Name} {creatorStudent.User.Surname}" : creatorUser.Name + " " + creatorUser.Surname;
                        var content = $"Zostałeś zaproszony do grupy {group.Name} przez {leaderName}";
                        var invitedUser = await _db.Users.FindAsync(targetStudent.UserId);
                        if (invitedUser != null)
                        {
                            notificationsToCreate.Add((invitedUser.Id, content, linkTarget));
                        }
                    }
                }
                else if (string.Equals(reqType, "ProjectRequest", StringComparison.OrdinalIgnoreCase) && dto.ProjectId.HasValue)
                {
                    var projectId = dto.ProjectId.Value;
                    var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
                    if (project != null && project.Company != null)
                    {
                        var companyUser = await _db.Users.FindAsync(project.Company.UserId);
                        if (companyUser != null)
                        {
                            var content = $"Grupa {group.Name} wysłała prośbę o realizację Twojego projektu: {project.Topic}";
                            notificationsToCreate.Add((companyUser.Id, content, linkTarget));
                            // assign requested project to group and mark as pending approval
                            group.ProjectId = projectId;
                            group.IsAccepted = GroupStatus.Pending;
                            _db.Groups.Update(group);
                        }
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Now that entity.Id exists and transaction is complete, create notifications with FK
                try
                {
                    foreach (var n in notificationsToCreate)
                    {
                        await _notifications.CreateNotificationAsync(n.userId, n.content, n.link, entity.Id);
                    }
                }
                catch
                {
                    // ignore notification creation errors — not critical for request creation
                }

                return CreatedAtAction(nameof(Post), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Problem("Nie udało się utworzyć prośby i powiadomień: " + ex.Message);
            }
        }

        [HttpPost("{id:int}/respond")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Respond(int id, [FromBody] RespondGroupRequestDto dto)
        {
            if (dto == null) return BadRequest();

            var req = await _db.GroupRequests.Include(r => r.Group).ThenInclude(g => g.Project).ThenInclude(p => p!.Company).FirstOrDefaultAsync(r => r.Id == id);
            if (req == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            // Check if user can respond: company owner or group leader
            if (req.Group?.Project?.Company?.UserId != currentUserId && req.Group?.LeaderId != currentUserId && !IsAdmin())
                return Forbid("No permission to respond to this request");

            if (req.Status == GroupStatus.Accepted || req.Status == GroupStatus.Declined)
                return BadRequest("Prośba została już rozpatrzona.");

            var group = await _db.Groups.Include(g => g.Project).Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == req.GroupId);

            var creatorUser = await _db.Users.FindAsync(req.CreatedByUserId);
            var creatorStudent = creatorUser != null ? await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == creatorUser.Id) : null;
            Student? targetStudent = null;
            if (req.StudentId.HasValue) targetStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == req.StudentId.Value);
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var action = dto.Action?.Trim().ToLowerInvariant();
            if (action != "accept" && action != "decline") return BadRequest("Akcja musi być 'accept' lub 'decline'.");

            // Authorization: who can respond depends on request type
            var reqType = req.Type?.Trim().ToLowerInvariant();
            var isAdmin = currentUser.Role == Models.Role.Admin;

            if (reqType == "projectrequest")
            {
                // only company owner (of the project) or admin can respond
                if (!req.ProjectId.HasValue) return BadRequest("Brak powiązanego projektu dla tej prośby.");
                var projectId = req.ProjectId.Value;
                var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null) return NotFound("Powiązany projekt nie został znaleziony.");
                if (!isAdmin)
                {
                    if (project.Company == null) return Forbid();
                    if (project.Company.UserId != currentUserId) return Forbid();
                }
            }
            else if (reqType == "invitation" || reqType == "invite" || reqType == "application")
            {
                // only group leader (for the group) or admin OR company (for applications where company responds) can respond
                var isLeader = false;
                if (group?.LeaderId is int leaderId)
                {
                    var leader = await _db.Students.FirstOrDefaultAsync(s => s.Id == leaderId);
                    if (leader != null && leader.UserId == currentUserId) isLeader = true;
                }
                if (!isAdmin && !isLeader)
                {
                    // allow company to respond to applications where they are the project owner
                    if (reqType == "application" && req.ProjectId.HasValue)
                    {
                        var projectId = req.ProjectId.Value;
                        var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
                        if (project == null) return NotFound("Powiązany projekt nie został znaleziony.");
                        if (project.Company == null || project.Company.UserId != currentUserId) return Forbid();
                    }
                    else
                    {
                        return Forbid();
                    }
                }
            }

            req.Status = action == "accept" ? GroupStatus.Accepted : GroupStatus.Declined;
            req.RespondedAt = DateTime.UtcNow;
            req.RespondedByUserId = currentUserId;

            if (action == "accept" && req.Type != null && (req.Type.Equals("invite", StringComparison.OrdinalIgnoreCase) || req.Type.Equals("invitation", StringComparison.OrdinalIgnoreCase)) && targetStudent != null)
            {
                var project = group?.Project ?? (group?.ProjectId.HasValue == true ? await _db.Projects.FindAsync(group?.ProjectId?.Value) : null);
                var currentMembers = group?.Members?.Count ?? (await _db.Students.CountAsync(s => s.GroupId == req.GroupId));
                if (project != null && currentMembers >= project.MaxNumberGroupMembers) return BadRequest($"Grupa ma już {currentMembers} członków, co przekracza maksymalny limit projektu ({project.MaxNumberGroupMembers}).");

                // Persist membership change directly via SQL to avoid tracking/merge issues in this flow
                var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Students\" SET group_id = {req.GroupId} WHERE id = {targetStudent.Id}");
                if (affected == 0)
                {
                    // fallback: update via EF if raw SQL didn't affect any row
                    targetStudent.GroupId = req.GroupId;
                    _db.Students.Update(targetStudent);
                    await _db.SaveChangesAsync();
                }
                // reload tracked entity so further queries in this context see updated value
                try
                {
                    await _db.Entry(targetStudent).ReloadAsync();
                }
                catch
                {
                    // ignore reload failures
                }
                // notify all current/future members that a student joined the group
                try
                {
                    var members = await _db.Students.Where(s => s.GroupId == req.GroupId).ToListAsync();
                    var joinerName = targetStudent.User != null ? $"{targetStudent.User.Name} {targetStudent.User.Surname}" : targetStudent.Email;
                    var contentJoin = $"Student {joinerName} dołączył do zespołu {group?.Name}.";
                    foreach (var m in members)
                    {
                        await _notifications.CreateNotificationAsync(m.UserId, contentJoin, $"group:{group?.Id}");
                    }
                }
                catch
                {
                    // ignore notification errors
                }
            }

            // notify relevant users
            try
            {
                if (req.Type != null && (
                    req.Type.Equals("join_request", StringComparison.OrdinalIgnoreCase)
                    || req.Type.Equals("application", StringComparison.OrdinalIgnoreCase)
                    || req.Type.Equals("Application", StringComparison.OrdinalIgnoreCase)))
                {
                    var project = group?.Project ?? (req.ProjectId.HasValue ? await _db.Projects.FindAsync(req.ProjectId.Value) : null);
                    var company = project != null ? await _db.Companies.FindAsync(project.CompanyId) : null;
                    var responderIsCompany = company != null && currentUserId == company.UserId;

                    if (responderIsCompany)
                    {
                        if (group != null)
                        {
                            var proj = group.Project ?? (group.ProjectId.HasValue ? await _db.Projects.FindAsync(group.ProjectId.Value) : null);
                            var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                            if (action == "accept" && proj != null && memberCount > proj.MaxNumberGroupMembers) return BadRequest($"Nie można zaakceptować grupy: ma {memberCount} członków, co przekracza maksymalny limit projektu ({proj.MaxNumberGroupMembers}).");

                            if (action == "accept" && req.ProjectId.HasValue && group.ProjectId != req.ProjectId.Value) group.ProjectId = req.ProjectId.Value;

                            group.IsAccepted = action == "accept" ? GroupStatus.Accepted : GroupStatus.Declined;
                            _db.Groups.Update(group);
                        }

                        if (group != null)
                        {
                            var members = await _db.Students.Where(s => s.GroupId == group.Id).ToListAsync();
                            foreach (var m in members)
                            {
                                var content = action == "accept" ? $"Zespół {group.Name} został przyjęty do projektu {project.Topic}." : $"Zespół {group.Name} nie został przyjęty do projektu {project.Topic}.";
                                await _notifications.CreateNotificationAsync(m.UserId, content, $"project:{project.Id}");
                            }
                        }
                    }
                    else
                    {
                        var notifyUserId = creatorStudent?.UserId ?? creatorUser?.Id ?? 0;
                        if (notifyUserId > 0)
                        {
                            var content = action == "accept" ? $"Twoja prośba o dołączenie do zespołu {group?.Name} została zaakceptowana." : $"Twoja prośba o dołączenie do zespołu {group?.Name} została odrzucona.";
                            await _notifications.CreateNotificationAsync(notifyUserId, content, $"group:{group?.Id}");
                        }
                    }
                }
                else if (req.Type != null && (
                    req.Type.Equals("invite", StringComparison.OrdinalIgnoreCase)
                    || req.Type.Equals("Invitation", StringComparison.OrdinalIgnoreCase)
                    || req.Type.Equals("invitation", StringComparison.OrdinalIgnoreCase)))
                {
                    var creator = await _db.Users.FindAsync(req.CreatedByUserId);
                    var content = action == "accept" ? $"Student {targetStudent?.User?.Name} {targetStudent?.User?.Surname} zaakceptował zaproszenie do zespołu {group?.Name}." : $"Student {targetStudent?.User?.Name} {targetStudent?.User?.Surname} odrzucił zaproszenie do zespołu {group?.Name}.";
                    if (creator != null) await _notifications.CreateNotificationAsync(creator.Id, content, $"group:{group?.Id}");
                }
            }
            catch
            {
                // ignore notification errors
            }

            _db.GroupRequests.Update(req);
            await _db.SaveChangesAsync();
            return Ok(req);
        }

        [HttpGet("byUser/{userId:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var requests = await _db.GroupRequests
                .Include(gr => gr.Group)
                .Include(gr => gr.Project)
                .Include(gr => gr.Student).ThenInclude(s => s!.User)
                .Include(gr => gr.CreatedByUser)
                .Where(gr => gr.CreatedByUserId == userId || (gr.Student != null && gr.Student.UserId == userId))
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("byProject/{projectId:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var requests = await _db.GroupRequests
                .Include(gr => gr.Group)
                .Include(gr => gr.Project)
                .Include(gr => gr.Student).ThenInclude(s => s!.User)
                .Include(gr => gr.CreatedByUser)
                .Where(gr => gr.ProjectId == projectId || (gr.Group != null && gr.Group.ProjectId == projectId))
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("byStudent/{studentId:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var requests = await _db.GroupRequests
                .Include(gr => gr.Group)
                .Include(gr => gr.Project)
                .Include(gr => gr.Student).ThenInclude(s => s!.User)
                .Include(gr => gr.CreatedByUser)
                .Where(gr => gr.StudentId == studentId)
                .ToListAsync();

            return Ok(requests);
        }

        // Validates and sanitizes CreateGroupRequestDto according to strict rules based on `Type`.
        // - Invitation: TargetEmail required, ProjectId forced to null.
        // - ProjectRequest: ProjectId required.
        // - Application: ProjectId forced to null; target student resolved to group's leader.
        private (bool IsValid, string? ErrorMessage) ValidateAndSanitizeDto(CreateGroupRequestDto dto, string reqType, Group group)
        {
            if (string.Equals(reqType, "Invitation", StringComparison.OrdinalIgnoreCase))
            {
                // Allow TargetEmail for invitations; ProjectId must be null.
                dto.ProjectId = null;
                if (string.IsNullOrWhiteSpace(dto.TargetEmail))
                    return (false, "Dla typu Invitation pole TargetEmail jest wymagane");
                return (true, null);
            }

            if (string.Equals(reqType, "ProjectRequest", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.ProjectId.HasValue)
                    return (false, "Dla typu ProjectRequest pole ProjectId jest wymagane");
                return (true, null);
            }

            if (string.Equals(reqType, "Application", StringComparison.OrdinalIgnoreCase))
            {
                dto.ProjectId = null;
                if (!group.LeaderId.HasValue)
                    return (false, "Wybrana grupa nie ma lidera; nie można złożyć Application.");
                return (true, null);
            }

            return (false, "Nieobsługiwany typ prośby. Użyj 'Invitation', 'ProjectRequest' lub 'Application'.");
        }
        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
            return roleClaim?.Value == "Admin";
        }
                private int GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(claim.Value);
        }
    }
}
