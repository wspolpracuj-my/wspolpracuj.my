using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            if (commentId <= 0) return BadRequest("commentId must be greater than 0");

            var exists = await _db.Comments.AnyAsync(c => c.Id == commentId);
            if (!exists) return NotFound();

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
        /// <summary>
        /// Tworzy nową odpowiedź na podstawie DTO.
        /// </summary>
        /// <param name="dto">Dane potrzebne do utworzenia odpowiedzi.</param>
        /// <returns>Utworzona odpowiedź z kodem 201 Created.</returns>
        public async Task<ActionResult<Response>> Post([FromBody] CreateResponseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var comment = await _db.Comments.FindAsync(dto.CommentId);
            if (comment == null) return NotFound($"Comment with id {dto.CommentId} not found.");

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"User with id {dto.UserId} not found.");

            var response = new Response
            {
                CommentId = dto.CommentId,
                UserId = dto.UserId,
                Content = dto.Content,
                CreatedAt = System.DateTime.UtcNow,
                Comment = comment,
                User = user
            };

            _db.Responses.Add(response);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetByComment), new { commentId = response.CommentId }, response);
        }
    }
}
