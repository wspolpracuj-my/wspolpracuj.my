using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;
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
        /// <summary>
        /// Tworzy kontroler odpowiedzi z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public ResponsesController(AppDbContext db) => _db = db;

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
            if (commentId <= 0) return BadRequest("Parametr commentId musi być większy niż 0.");

            var comment = await _db.Comments.Include(c => c.Project).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) return NotFound();

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
            if (!ModelState.IsValid) return BadRequest(ModelState);

<<<<<<< HEAD
            int currentUserId = GetCurrentUserId();
            if (dto.UserId != currentUserId) return Forbid("Cannot respond as another user");

            var comment = await _db.Comments.Include(c => c.Project).FirstOrDefaultAsync(c => c.Id == dto.CommentId);
            if (comment == null) return NotFound($"Comment with id {dto.CommentId} not found.");
=======
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            // prevent spoofing: set response author to current user
            dto.UserId = currentUserId;

            var comment = await _db.Comments.FindAsync(dto.CommentId);
            if (comment == null) return NotFound($"Komentarz o id {dto.CommentId} nie został znaleziony.");
>>>>>>> origin/StudentsApi+AuthByRole

            if (!await CanAccessProjectAsync(comment.ProjectId, currentUserId)) return Forbid("No access to this project");

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"Użytkownik o id {dto.UserId} nie został znaleziony.");

            var response = new Response
            {
                CommentId = dto.CommentId,
                UserId = dto.UserId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                Comment = comment,
                User = user
            };

            _db.Responses.Add(response);
            await _db.SaveChangesAsync();
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
            // Check if user is the company owner
            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;
            if (project.Company.UserId == userId) return true;

            // Check if user is a member of a group in the project
            var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == userId);
            if (student?.Group?.ProjectId == projectId) return true;

            return false;
        }
    }
}
