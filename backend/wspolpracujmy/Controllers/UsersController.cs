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
        /// <summary>
        /// Pobiera użytkownika po identyfikatorze.
        /// </summary>
        /// <param name="id">Id użytkownika.</param>
        /// <returns>DTO podsumowania użytkownika lub NotFound jeśli nie istnieje.</returns>
        public async Task<ActionResult<UserSummaryDto>> Get(int id)
        {
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

        [HttpPost]
        /// <summary>
        /// Tworzy nowego użytkownika w systemie.
        /// </summary>
        /// <param name="dto">Dane użytkownika do utworzenia.</param>
        /// <returns>DTO utworzonego użytkownika z kodem 201 Created.</returns>
        public async Task<ActionResult<UserSummaryDto>> Post([FromBody] CreateUserDto dto)
        {
            var user = new User
            {
                Name = dto.Name,
                Surname = dto.Surname,
                Role = dto.Role,
                Login = dto.Login,
                Password = dto.Password
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
        /// <summary>
        /// Usuwa użytkownika o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id użytkownika do usunięcia.</param>
        /// <returns>Brak treści (204) gdy usunięto, lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
