using System.Collections.Generic;
using System.Linq;
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
    /// Kontroler do zarządzania użytkownikami aplikacji.
    /// </summary>
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy kontroler użytkowników z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public UsersController(AppDbContext db) => _db = db;

        // [HttpGet]
        // Removed: returning all users without filters/pagination.
        // public async Task<IEnumerable<User>> Get() => await _db.Users.ToListAsync();

        [HttpGet("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Pobiera użytkownika po identyfikatorze.
        /// </summary>
        /// <param name="id">Id użytkownika.</param>
        /// <returns>DTO podsumowania użytkownika lub NotFound jeśli nie istnieje.</returns>
        public async Task<ActionResult<UserSummaryDto>> Get(int id)
        {
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            // Only admin or the user themself can view full user details
            if (currentUser.Role != Models.Role.Admin && currentUserId != id) return Forbid();

            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            return new UserSummaryDto
            {
                Id = u.Id,
                Name = u.Name,
                Surname = u.Surname,
                Role = u.Role,
                Login = u.Login
            };
        }

        [HttpGet("students")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Zwraca listę wszystkich studentów (name, surname, studentId, userId). Dostęp tylko dla admina.
        /// </summary>
        public async Task<ActionResult<IEnumerable<DTOs.StudentSummaryDto>>> GetStudents()
        {
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            if (currentUser.Role != Models.Role.Admin) return Forbid();

            var students = await _db.Students
                .Include(s => s.User)
                .ToListAsync();

            var result = students.Select(s => new DTOs.StudentSummaryDto
            {
                StudentId = s.Id,
                UserId = s.UserId,
                Name = s.User?.Name ?? string.Empty,
                Surname = s.User?.Surname ?? string.Empty
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Tworzy nowego użytkownika w systemie.
        /// </summary>
        /// <param name="dto">Dane użytkownika do utworzenia.</param>
        /// <returns>DTO utworzonego użytkownika z kodem 201 Created.</returns>
        public async Task<ActionResult<UserSummaryDto>> Post([FromBody] CreateUserDto dto)
        {
            // Only admin can create users through this endpoint
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();
            if (currentUser.Role != Models.Role.Admin) return Forbid();

            var user = new User
            {
                Name = dto.Name,
                Surname = dto.Surname,
                Role = dto.Role,
                Login = dto.Login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var summaryDto = new UserSummaryDto
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Role = user.Role,
                Login = user.Login
            };

            return CreatedAtAction(nameof(Get), new { id = user.Id }, summaryDto);
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Usuwa użytkownika o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id użytkownika do usunięcia.</param>
        /// <returns>Brak treści (204) gdy usunięto, lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            // only admin can delete users
            if (currentUser.Role != Models.Role.Admin) return Forbid();

            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            if (!IsAdmin()) return Forbid("Only admin can delete users");

            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            return NoContent();
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
    }
}
