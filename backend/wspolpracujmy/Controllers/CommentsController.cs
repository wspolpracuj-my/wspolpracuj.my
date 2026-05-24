using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do obsługi komentarzy projektów i ich odpowiedzi.
    /// </summary>
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly wspolpracujmy.Services.ProjectCommentService _projectCommentService;
        /// <summary>
        /// Tworzy instancję kontrolera komentarzy z zależnościami do bazy i serwisu.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        /// <param name="projectCommentService">Serwis obsługujący pobieranie komentarzy i odpowiedzi.</param>
        public CommentsController(AppDbContext db, wspolpracujmy.Services.ProjectCommentService projectCommentService)
        {
            _db = db;
            _projectCommentService = projectCommentService;
        }

        // [HttpGet]
        // Removed: return-all endpoint (use paginated/filtered endpoints instead)
        // public async Task<IEnumerable<Comment>> Get() => await _db.Comments.ToListAsync();

        [HttpGet("project/{projectId:int}")]
        /// <summary>
        /// Zwraca listę komentarzy wraz z odpowiedziami dla zadanego projektu.
        /// </summary>
        /// <param name="projectId">Identyfikator projektu.</param>
        /// <returns>Listę komentarzy z odpowiedziami dla projektu.</returns>
        public async Task<ActionResult<List<CommentWithResponsesDto>>> GetByProject(int projectId)
        {
            if (projectId <= 0) return BadRequest("Parametr projectId musi być większy niż 0.");

            var exists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!exists) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (!await CanAccessProjectAsync(projectId, currentUserId)) return Forbid();

            var comments = await _projectCommentService.GetCommentsForProjectAsync(projectId);
            return Ok(comments);
        }

        [HttpGet("project/{projectId:int}/groups/{groupId:int}")]
        /// <summary>
        /// Zwraca komentarze dla projektu przefiltrowane po konkretnej grupie.
        /// </summary>
        /// <param name="projectId">Identyfikator projektu.</param>
        /// <param name="groupId">Identyfikator grupy.</param>
        /// <returns>Listę komentarzy przypisanych do grupy w projekcie.</returns>
        public async Task<ActionResult<List<CommentWithResponsesDto>>> GetByProjectAndGroup(int projectId, int groupId)
        {
            if (projectId <= 0) return BadRequest("Parametr projectId musi być większy niż 0.");
            if (groupId <= 0) return BadRequest("Parametr groupId musi być większy niż 0.");

            var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists) return NotFound();

            var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId);
            if (!groupExists) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (!await CanAccessProjectAsync(projectId, currentUserId)) return Forbid();
            if (!await CanViewGroupCommentsAsync(projectId, groupId, currentUserId)) return Forbid();

            var comments = await _projectCommentService.GetCommentsForProjectByGroupAsync(projectId, groupId);
            return Ok(comments);
        }

        /*         [HttpGet("project/{projectId}")]
                public async Task<ActionResult<IEnumerable<Comment>>> GetByProjectId(int projectId)
                {
                    var comments = await _db.Comments
                        .Include(c => c.User)
                        .Where(c => c.ProjectId == projectId)
                        .ToListAsync();
                    return comments;
                } */

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Tworzy nowy komentarz na podstawie DTO i zapisuje go w bazie.
        /// </summary>
        /// <param name="dto">Dane potrzebne do utworzenia komentarza.</param>
        /// <returns>Utworzony komentarz z kodem 201 Created.</returns>
        public async Task<ActionResult<Comment>> Post([FromBody] CreateCommentDto dto)
        {
            if (dto == null) return BadRequest("Brak danych komentarza.");
            if (!ModelState.IsValid)
            {
                var validationMessage = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                return BadRequest(validationMessage ?? "Nieprawidłowe dane komentarza.");
            }
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("Treść komentarza jest wymagana.");
            if (dto.ProjectId <= 0)
                return BadRequest("Nieprawidłowy identyfikator projektu.");

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;
            var isAdmin = role == "Admin";
            var isCompany = string.Equals(role, "Company", StringComparison.OrdinalIgnoreCase)
                || role == ((int)Role.Company).ToString();

            if (!isAdmin && isCompany)
                return Forbid("Firma odpowiada na komentarze studentów, nie dodaje nowych wątków.");

            dto.UserId = currentUserId;

            if (!await CanAccessProjectAsync(dto.ProjectId, currentUserId))
                return Forbid("Brak dostępu do tego projektu — dołącz do zespołu lub złóż zgłoszenie.");

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null) return NotFound($"Projekt o id {dto.ProjectId} nie został znaleziony.");

            var user = await _db.Users.FindAsync(currentUserId);
            if (user == null) return NotFound($"Użytkownik o id {currentUserId} nie został znaleziony.");

            var comment = new Comment
            {
                ProjectId = dto.ProjectId,
                UserId = currentUserId,
                Content = dto.Content,
                CreatedAt = System.DateTime.UtcNow,
                Project = project,
                User = user
            };

            _db.Comments.Add(comment);

            // create notification for project owner (company.user)
            try
            {
                // try to find group name for the commenting user (if the user is a student in a group)
                var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == currentUserId);
                var groupName = student?.Group?.Name ?? "";

                var recipientUserId = project.Company?.UserId ?? 0;
                if (recipientUserId > 0)
                {
                    var teamName = !string.IsNullOrEmpty(groupName)
                        ? groupName
                        : $"Student {user.Name} {user.Surname}".Trim();
                    var content = Notification.FormatTeamCommentOnProject(teamName, project.Topic);
                    var groupId = student?.GroupId ?? 0;
                    var linkTarget = Notification.LinkTargetCompanyTeamComment(project.Id, groupId);

                    var recipientUser = await _db.Users.FindAsync(recipientUserId);
                    if (recipientUser != null)
                    {
                        var notification = new wspolpracujmy.Models.Notification
                        {
                            UserId = recipientUserId,
                            Content = content,
                            Status = wspolpracujmy.Models.NotificationStatus.NotRead,
                            User = recipientUser,
                            CreatedAt = System.DateTime.UtcNow,
                            LinkTarget = linkTarget
                        };

                        _db.Notifications.Add(notification);
                    }
                }
            }
            catch (System.Exception ex) when (!IsFatalException(ex))
            {
                // Swallow non-fatal notification errors so comment creation still succeeds
            }

            await _db.SaveChangesAsync();
            // Previously returned CreatedAtAction(nameof(Get), ...) but `Get` (return-all)
            // endpoint was removed. Use `GetByProject` (existing endpoint) so the
            // Location header points to a valid resource (comments for the project).
            return CreatedAtAction(nameof(GetByProject), new { projectId = comment.ProjectId }, comment);
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Usuwa komentarz o podanym identyfikatorze.
        /// Prawdopodobnie powinno nie istnieć a jedynie admin powienien mieć dostęp do czegoś takiego.
        /// </summary>
        /// <param name="id">Id komentarza do usunięcia.</param>
        /// <returns>Brak treści (204) gdy usunięto, lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Comments
                .Include(cm => cm.User)
                .Include(cm => cm.Project)
                .FirstOrDefaultAsync(cm => cm.Id == id);
            if (c == null) return NotFound();
            // Authorization: only Admin, comment owner, or owning company may delete
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;

            var isOwner = c.UserId == currentUserId;
            var isCompanyOwner = c.Project?.Company != null && c.Project.Company.UserId == currentUserId;
            if (!(role == "Admin" || isOwner || isCompanyOwner)) return Forbid();

            if (c.UserId != currentUserId && !IsAdmin()) return Forbid("No permission to delete this comment");

            // Best-effort: try to find unread notifications related to this comment's author
            // Notifications created for comments use LinkTarget = "project:{projectId}" and
            // the Content contains either "Student {Name} {Surname}" or "Zespół {groupName}".
            // We match unread notifications for the same project whose content mentions the author
            // full name or group name (if available) and remove them.
            try
            {
                var authorName = c.User != null ? (c.User.Name + " " + c.User.Surname).Trim() : string.Empty;
                var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == c.UserId);
                var groupName = student?.Group?.Name ?? string.Empty;

                var projectLink = c.Project != null ? $"project:{c.Project.Id}" : null;

                var query = _db.Notifications.Where(n => n.Status == NotificationStatus.NotRead);
                if (!string.IsNullOrEmpty(projectLink))
                    query = query.Where(n => n.LinkTarget == projectLink);

                if (!string.IsNullOrEmpty(authorName) && !string.IsNullOrEmpty(groupName))
                {
                    query = query.Where(n => EF.Functions.Like(n.Content, "%" + authorName + "%") || EF.Functions.Like(n.Content, "%" + groupName + "%"));
                }
                else if (!string.IsNullOrEmpty(authorName))
                {
                    query = query.Where(n => EF.Functions.Like(n.Content, "%" + authorName + "%"));
                }
                else if (!string.IsNullOrEmpty(groupName))
                {
                    query = query.Where(n => EF.Functions.Like(n.Content, "%" + groupName + "%"));
                }
                else
                {
                    // nothing identifiable — skip deleting notifications
                    _db.Comments.Remove(c);
                    await _db.SaveChangesAsync();
                    return Ok(new { commentDeleted = true, notificationsRemoved = 0, note = "Brak informacji o autorze/grupie do identyfikacji powiązanych powiadomień." });
                }

                var matches = await query.ToListAsync();
                if (matches.Count > 0)
                {
                    _db.Notifications.RemoveRange(matches);
                }
            }
            catch
            {
                // If anything goes wrong during notification cleanup, swallow and continue
            }

            _db.Comments.Remove(c);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        private int GetCurrentUserId()
        {
            if (!TryGetCurrentUserId(out var userId))
                throw new UnauthorizedAccessException("User not authenticated");
            return userId;
        }

        private bool IsAdmin()
        {
            var roleClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.Role);
            return roleClaim?.Value == "Admin";
        }

        private static bool IsFatalException(System.Exception ex)
        {
            return ex is System.OutOfMemoryException
                || ex is System.StackOverflowException
                || ex is System.AccessViolationException
                || ex is System.AppDomainUnloadedException
                || ex is System.BadImageFormatException
                || ex is System.CannotUnloadAppDomainException
                || ex is System.InvalidProgramException
                || ex is System.Threading.ThreadAbortException;
        }

        private async Task<bool> CanAccessProjectAsync(int projectId, int userId)
        {
            if (IsAdmin()) return true;

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;
            if (project.Company != null && project.Company.UserId == userId) return true;

            var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == userId);
            if (student?.GroupId == null) return false;

            if (student.Group?.ProjectId == projectId) return true;

            return await _db.GroupRequests.AnyAsync(gr =>
                gr.GroupId == student.GroupId
                && gr.ProjectId == projectId
                && gr.Type != null
                && EF.Functions.ILike(gr.Type, "projectrequest")
                && (gr.Status == GroupStatus.Pending || gr.Status == GroupStatus.Accepted));
        }

        private async Task<bool> CanViewGroupCommentsAsync(int projectId, int groupId, int userId)
        {
            if (IsAdmin()) return true;

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project?.Company.UserId == userId) return true;

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            return student?.GroupId == groupId;
        }
    }
}
