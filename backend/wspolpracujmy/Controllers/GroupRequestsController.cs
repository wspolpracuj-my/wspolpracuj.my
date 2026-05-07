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
    public class GroupRequestsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;

        public GroupRequestsController(AppDbContext db, NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GroupRequest>> Post([FromBody] CreateGroupRequestDto dto)
        {
            if (dto == null) return BadRequest();

            var group = await _db.Groups.Include(g => g.Project).Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == dto.GroupId);
            if (group == null) return NotFound($"Group with id {dto.GroupId} not found.");

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
                    return Forbid("Only the group leader can invite students.");

                var memberLimit = group.Project != null ? group.Project.MaxNumberGroupMembers : 5;
                var currentMembers = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                if (currentMembers >= memberLimit) return BadRequest($"Group already has {currentMembers} members which meets/exceeds the limit of {memberLimit}.");

                // dto.TargetStudentId was validated by sanitizer
                var invited = await _db.Students.FindAsync(dto.TargetStudentId.Value);
                if (invited == null) return NotFound($"Student with id {dto.TargetStudentId.Value} not found.");
                if (invited.GroupId.HasValue) return BadRequest("Invited student already belongs to a group.");
                targetStudentId = invited.Id;
            }
            else if (string.Equals(reqType, "ProjectRequest", StringComparison.OrdinalIgnoreCase))
            {
                if (creatorStudent == null || !group.LeaderId.HasValue || creatorStudent.Id != group.LeaderId.Value)
                    return Forbid("Only the group leader can request a project.");

                // dto.ProjectId was validated by sanitizer
                var project = await _db.Projects.FindAsync(dto.ProjectId.Value);
                if (project == null) return NotFound($"Project with id {dto.ProjectId.Value} not found.");

                if (project.MaxGroups.HasValue)
                {
                    var acceptedCount = await _db.Groups.CountAsync(g => g.ProjectId == project.Id && g.IsAccepted == GroupStatus.Accepted);
                    if (acceptedCount >= project.MaxGroups.Value) return BadRequest($"Project already reached max groups ({project.MaxGroups.Value}).");
                }

                if (group.ProjectId.HasValue) return BadRequest("Group is already assigned to a project.");

                targetStudentId = null;
            }
            else if (string.Equals(reqType, "Application", StringComparison.OrdinalIgnoreCase))
            {
                if (creatorStudent == null) return Forbid("Only students can apply to groups.");
                if (creatorStudent.GroupId.HasValue) return BadRequest("You already belong to a group.");

                // For Application the DTO TargetStudentId must point to the group's leader (sanitizer set this).
                if (!dto.TargetStudentId.HasValue) return BadRequest("Wybrana grupa nie ma lidera; nie można złożyć Application.");
                targetStudentId = dto.TargetStudentId.Value;
            }
            else
            {
                return BadRequest("Unsupported request type. Use 'Invitation', 'ProjectRequest' or 'Application'.");
            }

            // load student entity if present
            Student? targetStudent = null;
            if (targetStudentId.HasValue) targetStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == targetStudentId.Value);

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
                _db.GroupRequests.Add(entity);

                var createdAt = DateTime.UtcNow;
                const string linkTarget = "/requests";

                if (string.Equals(reqType, "Application", StringComparison.OrdinalIgnoreCase))
                {
                    // notify leader
                    if (group.LeaderId.HasValue)
                    {
                        var leader = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == group.LeaderId.Value);
                        if (leader != null)
                        {
                            var requesterName = creatorStudent?.User != null ? $"{creatorStudent.User.Name} {creatorStudent.User.Surname}" : creatorUser.Name + " " + creatorUser.Surname;
                            var content = $"Student {requesterName} chce dołączyć do Twojej grupy";
                            var leaderUser = await _db.Users.FindAsync(leader.UserId);
                            if (leaderUser != null)
                            {
                                _db.Notifications.Add(new Notification { UserId = leader.UserId, Content = content, Status = NotificationStatus.NotRead, User = leaderUser, CreatedAt = createdAt, LinkTarget = linkTarget });
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
                            _db.Notifications.Add(new Notification { UserId = invitedUser.Id, Content = content, Status = NotificationStatus.NotRead, User = invitedUser, CreatedAt = createdAt, LinkTarget = linkTarget });
                        }
                    }
                }
                else if (string.Equals(reqType, "ProjectRequest", StringComparison.OrdinalIgnoreCase))
                {
                    if (dto.ProjectId.HasValue)
                    {
                        var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == dto.ProjectId.Value);
                        if (project != null && project.Company != null)
                        {
                            var companyUser = await _db.Users.FindAsync(project.Company.UserId);
                            if (companyUser != null)
                            {
                                var content = $"Grupa {group.Name} wysłała prośbę o realizację Twojego projektu: {project.Topic}";
                                _db.Notifications.Add(new Notification { UserId = companyUser.Id, Content = content, Status = NotificationStatus.NotRead, User = companyUser, CreatedAt = createdAt, LinkTarget = linkTarget });
                                // assign requested project to group and mark as pending approval
                                group.ProjectId = dto.ProjectId.Value;
                                group.IsAccepted = GroupStatus.Pending;
                                _db.Groups.Update(group);
                            }
                        }
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return CreatedAtAction(nameof(Post), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Problem("Failed to create request and notification: " + ex.Message);
            }
        }

        [HttpPost("{id:int}/respond")]
        public async Task<IActionResult> Respond(int id, [FromBody] RespondGroupRequestDto dto)
        {
            if (dto == null) return BadRequest();

            var req = await _db.GroupRequests.FindAsync(id);
            if (req == null) return NotFound();

            if (req.Status == GroupStatus.Accepted || req.Status == GroupStatus.Declined)
                return BadRequest("Request already responded");

            var group = await _db.Groups.Include(g => g.Project).Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == req.GroupId);

            var creatorUser = await _db.Users.FindAsync(req.CreatedByUserId);
            var creatorStudent = creatorUser != null ? await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == creatorUser.Id) : null;
            Student? targetStudent = null;
            if (req.StudentId.HasValue) targetStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == req.StudentId.Value);

            var action = dto.Action?.Trim().ToLowerInvariant();
            if (action != "accept" && action != "decline") return BadRequest("Action must be 'accept' or 'decline'");

            req.Status = action == "accept" ? GroupStatus.Accepted : GroupStatus.Declined;
            req.RespondedAt = DateTime.UtcNow;

            if (action == "accept" && req.Type != null && req.Type.Equals("invite", StringComparison.OrdinalIgnoreCase))
            {
                if (targetStudent != null)
                {
                    var project = group?.Project ?? (group?.ProjectId.HasValue == true ? await _db.Projects.FindAsync(group.ProjectId.Value) : null);
                    var currentMembers = group?.Members?.Count ?? (await _db.Students.CountAsync(s => s.GroupId == req.GroupId));
                    if (project != null && currentMembers >= project.MaxNumberGroupMembers) return BadRequest($"Group already has {currentMembers} members which meets/exceeds project's max of {project.MaxNumberGroupMembers}.");

                    targetStudent.GroupId = req.GroupId;
                    _db.Students.Update(targetStudent);
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
                    var responderIsCompany = company != null && dto.RespondedByUserId == company.UserId;

                    if (responderIsCompany)
                    {
                        if (group != null)
                        {
                            var proj = group.Project ?? (group.ProjectId.HasValue ? await _db.Projects.FindAsync(group.ProjectId.Value) : null);
                            var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                            if (action == "accept" && proj != null && memberCount > proj.MaxNumberGroupMembers) return BadRequest($"Cannot accept group: it has {memberCount} members which exceeds project's max of {proj.MaxNumberGroupMembers}.");

                            if (action == "accept" && req.ProjectId.HasValue && group.ProjectId != req.ProjectId.Value) group.ProjectId = req.ProjectId.Value;

                            group.IsAccepted = action == "accept" ? GroupStatus.Accepted : GroupStatus.Declined;
                            _db.Groups.Update(group);
                        }

                        if (group != null)
                        {
                            var members = await _db.Students.Where(s => s.GroupId == group.Id).ToListAsync();
                            foreach (var m in members)
                            {
                                var content = action == "accept" ? $"Zespół {group.Name} został przyjęty do projektu {project?.Topic}." : $"Zespół {group.Name} nie został przyjęty do projektu {project?.Topic}.";
                                await _notifications.CreateNotificationAsync(m.UserId, content, $"project:{project?.Id}");
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
        public async Task<IActionResult> GetByUser(int userId)
        {
            var requests = await _db.GroupRequests
                .Include(gr => gr.Group)
                .Include(gr => gr.Project)
                .Include(gr => gr.Student).ThenInclude(s => s.User)
                .Include(gr => gr.CreatedByUser)
                .Where(gr => gr.CreatedByUserId == userId || (gr.Student != null && gr.Student.UserId == userId))
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("byProject/{projectId:int}")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var requests = await _db.GroupRequests
                .Include(gr => gr.Group)
                .Include(gr => gr.Project)
                .Include(gr => gr.Student).ThenInclude(s => s.User)
                .Include(gr => gr.CreatedByUser)
                .Where(gr => gr.ProjectId == projectId || (gr.Group != null && gr.Group.ProjectId == projectId))
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("byStudent/{studentId:int}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var requests = await _db.GroupRequests
                .Include(gr => gr.Group)
                .Include(gr => gr.Project)
                .Include(gr => gr.Student).ThenInclude(s => s.User)
                .Include(gr => gr.CreatedByUser)
                .Where(gr => gr.StudentId == studentId)
                .ToListAsync();

            return Ok(requests);
        }

        // Validates and sanitizes CreateGroupRequestDto according to strict rules based on `Type`.
        // - Invitation: TargetStudentId required, ProjectId forced to null.
        // - ProjectRequest: ProjectId required, TargetStudentId forced to null.
        // - Application: ProjectId forced to null, TargetStudentId set to group's leader id (must exist).
        private (bool IsValid, string? ErrorMessage) ValidateAndSanitizeDto(CreateGroupRequestDto dto, string reqType, Group group)
        {
            if (string.Equals(reqType, "Invitation", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.TargetStudentId.HasValue)
                    return (false, "Dla typu Invitation pole TargetStudentId jest wymagane");
                dto.ProjectId = null;
                return (true, null);
            }

            if (string.Equals(reqType, "ProjectRequest", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.ProjectId.HasValue)
                    return (false, "Dla typu ProjectRequest pole ProjectId jest wymagane");
                dto.TargetStudentId = null;
                return (true, null);
            }

            if (string.Equals(reqType, "Application", StringComparison.OrdinalIgnoreCase))
            {
                dto.ProjectId = null;
                if (!group.LeaderId.HasValue)
                    return (false, "Wybrana grupa nie ma lidera; nie można złożyć Application.");
                dto.TargetStudentId = group.LeaderId.Value;
                return (true, null);
            }

            return (false, "Unsupported request type. Use 'Invitation', 'ProjectRequest' or 'Application'.");
        }
    }
}
