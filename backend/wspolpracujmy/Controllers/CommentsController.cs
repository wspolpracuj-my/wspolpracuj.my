using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
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
        [Microsoft.AspNetCore.Authorization.Authorize]
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

            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            // Admin has access to all comments
            if (role == "Admin")
            {
                var comments = await _projectCommentService.GetCommentsForProjectAsync(projectId);
                return Ok(comments);
            }

            // Company: only the company owning the project may see all comments
            if (role == "Company")
            {
                var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null) return NotFound();
                if (project.Company == null || project.Company.UserId != currentUserId) return Forbid();

                var comments = await _projectCommentService.GetCommentsForProjectAsync(projectId);
                return Ok(comments);
            }

            // Students: can see comments written by the company OR comments written by their own group.
            // For those comments, include responses written by the company and responses written by members of their group.
            if (role == "Student")
            {
                var currentStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                if (currentStudent == null) return Forbid();
                var groupId = currentStudent.GroupId;

                var comments = await _db.Comments
                    .Where(c => c.ProjectId == projectId && (
                        c.User.Role == Models.Role.Company
                        || (groupId.HasValue && _db.Students.Any(s => s.UserId == c.UserId && s.GroupId == groupId))
                    ))
                    .Include(c => c.User)
                    .Include(c => c.Responses).ThenInclude(r => r.User).ThenInclude(u => u.Student)
                    .Select(c => new CommentWithResponsesDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User != null ? c.User.Name + " " + c.User.Surname : null,
                        GroupId = _db.Students.Where(s => s.UserId == c.UserId).Select(s => (int?)s.GroupId).FirstOrDefault(),
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        Responses = c.Responses
                            .Where(r => r.User != null && (
                                (r.User.Student != null && r.User.Student.GroupId.HasValue && groupId.HasValue && r.User.Student.GroupId == groupId)
                                || r.User.Role == Models.Role.Company
                            ))
                            .Select(r => new ResponseDto
                            {
                                Id = r.Id,
                                UserId = r.UserId,
                                UserName = r.User != null ? r.User.Name + " " + r.User.Surname : null,
                                Content = r.Content,
                                CreatedAt = r.CreatedAt
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(comments);
            }

            // Other roles are not allowed
            return Forbid();
        }

        [HttpGet("project/{projectId:int}/groups/{groupId:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
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

            var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId && g.ProjectId == projectId);
            if (!groupExists) return NotFound();

            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            // Admin: see all comments
            if (role == "Admin")
            {
                var comments = await _projectCommentService.GetCommentsForProjectByGroupAsync(projectId, groupId);
                return Ok(comments);
            }

            // Company: only owning company may see all
            if (role == "Company")
            {
                var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
                if (project == null) return NotFound();
                if (project.Company == null || project.Company.UserId != currentUserId) return Forbid();

                var comments = await _projectCommentService.GetCommentsForProjectByGroupAsync(projectId, groupId);
                return Ok(comments);
            }

            // Student: may view only if they belong to this group, and even then only company comments + responses by group members
            if (role == "Student")
            {
                var currentStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                if (currentStudent == null) return Forbid();
                if (!currentStudent.GroupId.HasValue || currentStudent.GroupId.Value != groupId) return Forbid();

                var comments = await _db.Comments
                    .Where(c => c.ProjectId == projectId && (
                        c.User.Role == Models.Role.Company
                        || _db.Students.Any(s => s.UserId == c.UserId && s.GroupId == groupId)
                    ))
                    .Include(c => c.User)
                    .Include(c => c.Responses).ThenInclude(r => r.User).ThenInclude(u => u.Student)
                    .Select(c => new CommentWithResponsesDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User != null ? c.User.Name + " " + c.User.Surname : null,
                        GroupId = _db.Students.Where(s => s.UserId == c.UserId).Select(s => (int?)s.GroupId).FirstOrDefault(),
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        Responses = c.Responses
                            .Where(r => r.User != null && (
                                (r.User.Student != null && r.User.Student.GroupId.HasValue && r.User.Student.GroupId == groupId)
                                || r.User.Role == Models.Role.Company
                            ))
                            .Select(r => new ResponseDto
                            {
                                Id = r.Id,
                                UserId = r.UserId,
                                UserName = r.User != null ? r.User.Name + " " + r.User.Surname : null,
                                Content = r.Content,
                                CreatedAt = r.CreatedAt
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(comments);
            }

            return Forbid();
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            // authenticate and ensure caller matches dto.UserId (or is Admin)
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            // For non-admins, force comment.UserId to current user and prevent spoofing
            if (role != "Admin") dto.UserId = currentUserId;

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null) return NotFound($"Projekt o id {dto.ProjectId} nie został znaleziony.");

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"Użytkownik o id {dto.UserId} nie został znaleziony.");

            var comment = new Comment
            {
                ProjectId = dto.ProjectId,
                UserId = dto.UserId,
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
                var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == dto.UserId);
                var groupName = student?.Group?.Name ?? "";

                var recipientUserId = project.Company?.UserId ?? 0;
                if (recipientUserId > 0)
                {
                    var content = string.Empty;
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        content = $"Zespół {groupName} napisał komentarz pod projektem {project.Topic}.";
                    }
                    else
                    {
                        // fallback to user's full name
                        content = $"Student {user.Name} {user.Surname} napisał komentarz pod projektem {project.Topic}.";
                    }

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
                            LinkTarget = $"project:{project.Id}"
                        };

                        _db.Notifications.Add(notification);
                    }
                }
            }
            catch
            {
                // Swallow notification errors so comment creation still succeeds
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
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            var isOwner = c.UserId == currentUserId;
            var isCompanyOwner = c.Project?.Company != null && c.Project.Company.UserId == currentUserId;
            if (!(role == "Admin" || isOwner || isCompanyOwner)) return Forbid();

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
    }
}
