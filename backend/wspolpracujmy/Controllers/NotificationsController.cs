using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do obsługi powiadomień użytkowników (wyłącznie tekst do wyświetlenia).
    /// </summary>
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly wspolpracujmy.Services.NotificationService _notifications;

        public NotificationsController(AppDbContext db, wspolpracujmy.Services.NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpGet("all")]
        /// <summary>
        /// Zwraca listę WSZYSTKICH powiadomień w systemie (tylko dla administratora).
        /// Każde powiadomienie zawiera odbiorcę, treść oraz najlepszą próbę wskazania nadawcy.
        /// </summary>
        public async Task<ActionResult<IEnumerable<AdminNotificationDto>>> GetAllForAdmin()
        {
            if (!IsAdmin()) return Forbid();

            var entities = await _db.Notifications
                .Include(n => n.User)
                .Include(n => n.GroupRequest).ThenInclude(gr => gr!.CreatedByUser)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var result = new List<AdminNotificationDto>();
            foreach (var entity in entities)
            {
                var displayContent = await FormatDisplayContentAsync(entity);
                var (fromUserId, fromName) = await ResolveSenderAsync(entity);

                result.Add(new AdminNotificationDto
                {
                    Id = entity.Id,
                    ToUserId = entity.UserId,
                    ToName = entity.User != null
                        ? $"{entity.User.Name} {entity.User.Surname}".Trim()
                        : $"Użytkownik #{entity.UserId}",
                    FromUserId = fromUserId,
                    FromName = fromName,
                    Content = displayContent,
                    IsRead = entity.Status == NotificationStatus.Read,
                    CreatedAt = entity.CreatedAt
                });
            }

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<NotificationDto>> Get(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();
            if (n.UserId != currentUserId && !IsAdmin())
                return Forbid();

            return Ok(MapToDto(n, await FormatDisplayContentAsync(n)));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetForUser([FromQuery] int? userId = null)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var targetUserId = userId ?? currentUserId;
            if (userId.HasValue && targetUserId != currentUserId && !IsAdmin())
                return Forbid("Brak uprawnień do przeglądania powiadomień innego użytkownika.");

            var isStudent = await _db.Students.AnyAsync(s => s.UserId == targetUserId);
            var isCompany = await _db.Companies.AnyAsync(c => c.UserId == targetUserId);

            var entities = await _db.Notifications
                .Where(n => n.UserId == targetUserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            if (isStudent)
            {
                entities = entities
                    .Where(n => Notification.IsStudentDisplayNotification(n.LinkTarget, n.Content))
                    .ToList();
            }
            else if (isCompany)
            {
                entities = entities
                    .Where(n => Notification.IsCompanyDisplayNotification(n.LinkTarget, n.Content))
                    .ToList();
            }

            var result = new List<NotificationDto>();
            foreach (var entity in entities)
            {
                var content = await FormatDisplayContentAsync(entity, isStudent, isCompany);
                result.Add(MapToDto(entity, content));
            }

            return Ok(result);
        }

        /// <summary>
        /// Alias używany przez starszy frontend firmy — zwraca powiadomienia zalogowanego użytkownika firmy.
        /// </summary>
        [HttpGet("company/{companyId:int}")]
        [Authorize(Policy = "CompanyOnly")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetForCompany(int companyId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null) return NotFound();
            if (company.UserId != currentUserId && !IsAdmin())
                return Forbid();

            return await GetForUser(company.UserId);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<NotificationDto>> Post(Notification notification)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var role = User?.FindFirst(ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;
            if (role != "Admin")
                notification.UserId = currentUserId;

            var created = await _notifications.CreateNotificationAsync(
                notification.UserId,
                notification.Content,
                notification.LinkTarget,
                notification.GroupRequestId);

            return CreatedAtAction(nameof(Get), new { id = created.Id }, MapToDto(created, created.Content));
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return BadRequest(new { message = "Tablica 'ids' jest wymagana." });

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            await _notifications.MarkAsReadForUserAsync(currentUserId, ids);
            return NoContent();
        }

        private static NotificationDto MapToDto(Notification entity, string displayContent)
        {
            return new NotificationDto
            {
                Id = entity.Id,
                Content = displayContent,
                IsRead = entity.Status == NotificationStatus.Read,
                CreatedAt = entity.CreatedAt
            };
        }

        private async Task<string> FormatDisplayContentAsync(
            Notification notification,
            bool? isStudent = null,
            bool? isCompany = null)
        {
            if (!isStudent.HasValue && !isCompany.HasValue)
            {
                isStudent = await _db.Students.AnyAsync(s => s.UserId == notification.UserId);
                isCompany = !isStudent.Value && await _db.Companies.AnyAsync(c => c.UserId == notification.UserId);
            }

            if (isCompany == true)
                return await FormatCompanyContentAsync(notification);

            if (isStudent == true)
                return await FormatStudentContentAsync(notification);

            return notification.Content;
        }

        private async Task<string> FormatCompanyContentAsync(Notification notification)
        {
            if (notification.Content.StartsWith("Zespół ", System.StringComparison.OrdinalIgnoreCase)
                && (notification.Content.Contains("napisał komentarz", StringComparison.OrdinalIgnoreCase)
                    || notification.Content.Contains("zgłosił się do projektu", StringComparison.OrdinalIgnoreCase)))
            {
                return notification.Content;
            }

            var parsed = TryParseCompanyLink(notification.LinkTarget);
            string teamName = "Zespół";
            string projectName = "projekt";

            if (parsed.ProjectId.HasValue)
            {
                var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == parsed.ProjectId.Value);
                projectName = project?.Topic ?? projectName;
            }

            if (parsed.GroupId.HasValue && parsed.GroupId.Value > 0)
            {
                var group = await _db.Groups.FindAsync(parsed.GroupId.Value);
                if (group != null) teamName = group.Name;
            }
            else if (notification.GroupRequestId.HasValue)
            {
                var request = await _db.GroupRequests
                    .Include(gr => gr.Group)
                    .FirstOrDefaultAsync(gr => gr.Id == notification.GroupRequestId.Value);
                if (request?.Group != null) teamName = request.Group.Name;
            }
            else
            {
                teamName = TryExtractTeamNameFromLegacyContent(notification.Content) ?? teamName;
                projectName = TryExtractProjectNameFromLegacyContent(notification.Content) ?? projectName;
            }

            if (notification.LinkTarget != null
                && notification.LinkTarget.StartsWith(Notification.CompanyTeamCommentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Notification.FormatTeamCommentOnProject(teamName, projectName);
            }

            if (notification.LinkTarget != null
                && notification.LinkTarget.StartsWith(Notification.CompanyTeamApplicationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Notification.FormatTeamProjectApplication(teamName, projectName);
            }

            if (notification.Content.Contains("komentarz", StringComparison.OrdinalIgnoreCase))
                return Notification.FormatTeamCommentOnProject(teamName, projectName);

            if (notification.Content.Contains("prośbę", StringComparison.OrdinalIgnoreCase)
                || notification.Content.Contains("zgłosi", StringComparison.OrdinalIgnoreCase))
            {
                return Notification.FormatTeamProjectApplication(teamName, projectName);
            }

            return notification.Content;
        }

        private static (int? ProjectId, int? GroupId) TryParseCompanyLink(string? linkTarget)
        {
            if (string.IsNullOrWhiteSpace(linkTarget))
                return (null, null);

            if (linkTarget.StartsWith("project:", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(linkTarget["project:".Length..], out var legacyProjectId))
            {
                return (legacyProjectId, null);
            }

            foreach (var prefix in new[] { Notification.CompanyTeamCommentPrefix, Notification.CompanyTeamApplicationPrefix })
            {
                if (!linkTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var payload = linkTarget[prefix.Length..];
                var parts = payload.Split(':');
                int? projectId = parts.Length > 0 && int.TryParse(parts[0], out var p) ? p : null;
                int? groupId = parts.Length > 1 && int.TryParse(parts[1], out var g) && g > 0 ? g : null;
                return (projectId, groupId);
            }

            return (null, null);
        }

        private static string? TryExtractTeamNameFromLegacyContent(string content)
        {
            if (content.StartsWith("Zespół ", StringComparison.OrdinalIgnoreCase))
            {
                var rest = content["Zespół ".Length..];
                var end = rest.IndexOf(" napisał", StringComparison.OrdinalIgnoreCase);
                if (end < 0) end = rest.IndexOf(" zgłosił", StringComparison.OrdinalIgnoreCase);
                if (end > 0) return rest[..end].Trim();
            }

            if (content.StartsWith("Grupa ", StringComparison.OrdinalIgnoreCase))
            {
                var rest = content["Grupa ".Length..];
                var end = rest.IndexOf(" wysłała", StringComparison.OrdinalIgnoreCase);
                if (end > 0) return rest[..end].Trim();
            }

            return null;
        }

        private static string? TryExtractProjectNameFromLegacyContent(string content)
        {
            const string underProject = "pod projektem ";
            var idx = content.IndexOf(underProject, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var name = content[(idx + underProject.Length)..].TrimEnd('.');
                if (!string.IsNullOrEmpty(name)) return name;
            }

            const string colonProject = "projektu: ";
            idx = content.IndexOf(colonProject, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var name = content[(idx + colonProject.Length)..].TrimEnd('.');
                if (!string.IsNullOrEmpty(name)) return name;
            }

            return null;
        }

        private async Task<string> FormatStudentContentAsync(Notification notification)
        {
            if (notification.Content.StartsWith("FIRMA ", System.StringComparison.OrdinalIgnoreCase))
                return notification.Content;

            var projectId = TryParseProjectIdFromLink(notification.LinkTarget);
            if (!projectId.HasValue)
                return notification.Content;

            var project = await _db.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == projectId.Value);

            var companyName = project?.Company?.CompanyName ?? "Firma";
            var projectName = project?.Topic ?? "projekt";

            if (notification.LinkTarget != null
                && notification.LinkTarget.StartsWith(Notification.StudentCommentReplyPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return Notification.FormatCompanyCommentReply(companyName, projectName);
            }

            if (notification.LinkTarget != null
                && notification.LinkTarget.StartsWith(Notification.StudentProjectDecisionPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                var accepted = notification.LinkTarget.EndsWith(":accept", System.StringComparison.OrdinalIgnoreCase)
                    || notification.Content.Contains("przyjęty", System.StringComparison.OrdinalIgnoreCase)
                    || notification.Content.Contains("zaakceptowała", System.StringComparison.OrdinalIgnoreCase);

                return accepted
                    ? Notification.FormatCompanyProjectAccepted(companyName, projectName)
                    : Notification.FormatCompanyProjectDeclined(companyName, projectName);
            }

            if (notification.Content.Contains("nie został przyjęty", System.StringComparison.OrdinalIgnoreCase)
                || notification.Content.Contains("odrzucona", System.StringComparison.OrdinalIgnoreCase)
                || notification.Content.Contains("odrzuciła", System.StringComparison.OrdinalIgnoreCase))
            {
                return Notification.FormatCompanyProjectDeclined(companyName, projectName);
            }

            if (notification.Content.Contains("przyjęty", System.StringComparison.OrdinalIgnoreCase)
                || notification.Content.Contains("zaakceptowała", System.StringComparison.OrdinalIgnoreCase))
            {
                return Notification.FormatCompanyProjectAccepted(companyName, projectName);
            }

            return notification.Content;
        }

        private static int? TryParseProjectIdFromLink(string? linkTarget)
        {
            if (string.IsNullOrWhiteSpace(linkTarget))
                return null;

            if (linkTarget.StartsWith("project:", System.StringComparison.OrdinalIgnoreCase)
                && int.TryParse(linkTarget["project:".Length..], out var legacyId))
            {
                return legacyId;
            }

            if (linkTarget.StartsWith(Notification.StudentCommentReplyPrefix, System.StringComparison.OrdinalIgnoreCase)
                && int.TryParse(linkTarget[Notification.StudentCommentReplyPrefix.Length..], out var commentProjectId))
            {
                return commentProjectId;
            }

            if (linkTarget.StartsWith(Notification.StudentProjectDecisionPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                var payload = linkTarget[Notification.StudentProjectDecisionPrefix.Length..];
                var colon = payload.IndexOf(':');
                var idPart = colon >= 0 ? payload[..colon] : payload;
                if (int.TryParse(idPart, out var decisionProjectId))
                    return decisionProjectId;
            }

            return null;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        private int GetCurrentUserId()
        {
            if (!TryGetCurrentUserId(out var userId))
                throw new System.UnauthorizedAccessException("User not authenticated");
            return userId;
        }

        private bool IsAdmin()
        {
            var roleClaim = User?.FindFirst(ClaimTypes.Role);
            return roleClaim?.Value == "Admin";
        }

        /// <summary>
        /// Próbuje wskazać nadawcę powiadomienia. Najpierw analizuje powiązane
        /// żądanie grupy (GroupRequest.CreatedByUser), następnie próbuje wnioskować
        /// na podstawie treści (firma właściciela projektu lub lider grupy).
        /// </summary>
        private async Task<(int? userId, string? name)> ResolveSenderAsync(Notification notification)
        {
            if (notification.GroupRequest?.CreatedByUser != null)
            {
                var sender = notification.GroupRequest.CreatedByUser;
                return (sender.Id, $"{sender.Name} {sender.Surname}".Trim());
            }

            var projectId = TryParseProjectIdFromLink(notification.LinkTarget)
                ?? TryParseProjectIdFromCompanyLink(notification.LinkTarget);

            if (projectId.HasValue)
            {
                if (notification.Content.StartsWith("FIRMA ", StringComparison.OrdinalIgnoreCase))
                {
                    var project = await _db.Projects
                        .Include(p => p.Company).ThenInclude(c => c.User)
                        .FirstOrDefaultAsync(p => p.Id == projectId.Value);
                    var owner = project?.Company?.User;
                    if (owner != null)
                    {
                        return (owner.Id, project!.Company.CompanyName);
                    }
                }

                if (notification.Content.StartsWith("Zespół ", StringComparison.OrdinalIgnoreCase)
                    || notification.Content.StartsWith("Grupa ", StringComparison.OrdinalIgnoreCase))
                {
                    var (_, groupId) = TryParseCompanyLink(notification.LinkTarget);
                    if (groupId.HasValue && groupId.Value > 0)
                    {
                        var group = await _db.Groups
                            .Include(g => g.Leader).ThenInclude(s => s!.User)
                            .FirstOrDefaultAsync(g => g.Id == groupId.Value);
                        var leaderUser = group?.Leader?.User;
                        if (leaderUser != null)
                        {
                            return (leaderUser.Id, $"{leaderUser.Name} {leaderUser.Surname}".Trim());
                        }
                    }
                }
            }

            // Bez dodatkowych danych: traktuj jako informację systemową.
            return (null, "System");
        }

        private static int? TryParseProjectIdFromCompanyLink(string? linkTarget)
        {
            if (string.IsNullOrWhiteSpace(linkTarget)) return null;
            var (projectId, _) = TryParseCompanyLink(linkTarget);
            return projectId;
        }
    }
}
