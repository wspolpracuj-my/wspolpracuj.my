using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;
using wspolpracujmy.Services;
using Microsoft.AspNetCore.Authorization;


namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do zarządzania odpowiedziami na komentarze.
    /// </summary>
    public class ResponsesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;

        public ResponsesController(AppDbContext db, NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        // [HttpGet]
        // Removed: return-all endpoint (use GET /api/responses/comment/{commentId} instead)
        // public async Task<IEnumerable<Response>> Get() => await _db.Responses.ToListAsync();

        [HttpGet("comment/{commentId:int}")]
        /// <summary>
        /// Zwraca listę odpowiedzi przypisanych do konkretnego komentarza.
        /// </summary>
        /// <param name="commentId">Id komentarza.</param>
        /// <returns>Lista DTO odpowiedzi.</returns>
        public async Task<ActionResult<List<ResponseDto>>> GetByComment(int commentId)
        {
            if (commentId <= 0) return BadRequest(new { error = "Nieprawidłowy numer komentarza." });

            var comment = await _db.Comments.Include(c => c.Project).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) return NotFound(new { error = "Komentarz nie został znaleziony." });

            int currentUserId = GetCurrentUserId();
            if (!await CanAccessProjectAsync(comment.ProjectId, currentUserId)) return Forbid();

            var responses = await _db.Responses
                .Where(r => r.CommentId == commentId)
                .Include(r => r.User)
                .Select(r => new ResponseDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User.Name + " " + r.User.Surname,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(responses);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Tworzy nową odpowiedź na podstawie DTO.
        /// </summary>
        /// <param name="dto">Dane potrzebne do utworzenia odpowiedzi.</param>
        /// <returns>Utworzona odpowiedź z kodem 201 Created.</returns>
        public async Task<ActionResult<Response>> Post([FromBody] CreateResponseDto dto)
        {
            if (dto == null) return BadRequest("Brak danych odpowiedzi.");
            if (dto.CommentId <= 0) return BadRequest("Nieprawidłowy identyfikator komentarza.");
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("Treść odpowiedzi jest wymagana.");

            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? User?.FindFirst("role")?.Value;
            var isAdmin = role == "Admin";
            var isCompany = string.Equals(role, "Company", StringComparison.OrdinalIgnoreCase)
                || role == ((int)Role.Company).ToString();

            if (!isAdmin && !isCompany)
                return Forbid("Tylko firma może odpowiadać na komentarze studentów.");

            dto.UserId = currentUserId;

            var comment = await _db.Comments
                .Include(c => c.Project)
                    .ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(c => c.Id == dto.CommentId);
            if (comment == null) return NotFound($"Komentarz o id {dto.CommentId} nie został znaleziony.");

            if (!isAdmin && (comment.Project?.Company == null || comment.Project.Company.UserId != currentUserId))
                return Forbid("Możesz odpowiadać tylko na komentarze pod swoimi projektami.");

            var user = await _db.Users.FindAsync(currentUserId);
            if (user == null) return NotFound($"Użytkownik o id {dto.UserId} nie został znaleziony.");

            var response = new Response
            {
                CommentId = dto.CommentId,
                UserId = currentUserId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                Comment = comment,
                User = user
            };

            _db.Responses.Add(response);
            await _db.SaveChangesAsync();

            try
            {
                var companyName = comment.Project?.Company?.CompanyName ?? "Firma";
                var projectName = comment.Project?.Topic ?? "projekt";
                var notifyContent = Notification.FormatCompanyCommentReply(companyName, projectName);
                var linkTarget = Notification.LinkTargetCommentReply(comment.ProjectId);
                await _notifications.CreateNotificationAsync(comment.UserId, notifyContent, linkTarget);
            }
            catch
            {
                // Powiadomienie nie jest krytyczne dla zapisu odpowiedzi.
            }

            return CreatedAtAction(nameof(GetByComment), new { commentId = response.CommentId }, response);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(claim.Value);
        }

        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
            return roleClaim?.Value == "Admin";
        }

        private async Task<bool> CanAccessProjectAsync(int projectId, int userId)
        {
            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;
            if (project.Company.UserId == userId) return true;

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
    }
}
