using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using Npgsql;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Kontroler do zarządzania danymi studentów.
    /// </summary>
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy kontroler studentów z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public StudentsController(AppDbContext db) => _db = db;

        [HttpGet("byEmail")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<StudentDto>> GetByEmail([FromQuery] string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return BadRequest("Parametr zapytania 'email' jest wymagany.");
            var e = email.Trim().ToLowerInvariant();
            var student = await _db.Students
                .Where(s => s.Email.ToLower() == e)
                .Select(s => new StudentDto { Id = s.Id, UserId = s.UserId, GroupId = s.GroupId, Email = s.Email })
                .FirstOrDefaultAsync();
            if (student == null) return NotFound();
            return Ok(student);
        }

        // [HttpGet]
        // Removed: returning all students without filters/pagination.
        // public async Task<IEnumerable<Student>> Get() => await _db.Students.ToListAsync();

        // [HttpGet("{id:int}")]
        // /// <summary>
        // /// Pobiera studenta po identyfikatorze.
        // /// </summary>
        // /// <param name="id">Id studenta.</param>
        // /// <returns>Obiekt studenta lub NotFound jeśli nie istnieje.</returns>
        // public async Task<ActionResult<Student>> Get(int id)
        // {
        //     var s = await _db.Students.FindAsync(id);
        //     if (s == null) return NotFound();
        //     return s;
        // }

        [HttpGet("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Pobiera studenta po identyfikatorze w postaci DTO.
        /// </summary>
        /// <param name="id">Id studenta.</param>
        /// <returns>StudentDto lub NotFound jeśli nie istnieje.</returns>
        public async Task<ActionResult<StudentDto>> Get(int id)
        {
            var student = await _db.Students
                .Where(s => s.Id == id)
                .Select(s => new StudentDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    GroupId = s.GroupId,
                    Email = s.Email
                })
                .FirstOrDefaultAsync();

            if (student == null) return NotFound();
            return Ok(student);
        }

        // [HttpPost]
        // /// <summary>
        // /// Tworzy nowego studenta w systemie.
        // /// </summary>
        // /// <param name="student">Obiekt studenta do utworzenia.</param>
        // /// <returns>Utworzony student z kodem 201 Created.</returns>
        // public async Task<ActionResult<Student>> Post(Student student)
        // {
        //     _db.Students.Add(student);
        //     await _db.SaveChangesAsync();
        //     return CreatedAtAction(nameof(Get), new { id = student.Id }, student);
        // }

        /// <summary>
        /// DTO używane do zmiany przypisania studenta do grupy.
        /// </summary>
        public class ChangeStudentGroupDto { public int GroupId { get; set; } }

        [HttpPatch("{id:int}/group")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Zmienia grupę, do której należy student.
        /// </summary>
        /// <param name="id">Id studenta.</param>
        /// <param name="dto">DTO zawierające nowe Id grupy.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> PatchGroup(int id, [FromBody] ChangeStudentGroupDto dto)
        {
            if (dto == null) return BadRequest();

            var student = await _db.Students.FindAsync(id);
            if (student == null) return NotFound();

            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) return BadRequest(new { error = "Nie znaleziono grupy." });

            student.GroupId = dto.GroupId;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Usuwa studenta o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id studenta do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Students.AsNoTracking().Where(st => st.Id == id).FirstOrDefaultAsync();
            if (s == null) return NotFound();

            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.AsNoTracking().Where(u => u.Id == currentUserId).FirstOrDefaultAsync();
            if (currentUser == null) return Unauthorized();

            if (currentUser.Role != Role.Admin) return Forbid();

            // If the student is a leader, for each group either assign the next member as leader
            // or remove the group. To avoid EF circular dependency, persist intermediate
            // changes with multiple SaveChangesAsync calls:
            var groupsLed = await _db.Groups.Where(g => g.LeaderId == s.Id).ToListAsync();

            foreach (var group in groupsLed)
            {
                var nextMember = await _db.Students
                    .Where(st => st.GroupId == group.Id && st.Id != s.Id)
                    .OrderBy(st => st.Id)
                    .FirstOrDefaultAsync();

                if (nextMember != null)
                {
                    group.LeaderId = nextMember.Id;
                    _db.Groups.Update(group);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // No other members -> unset leader, commit, then remove group
                    group.LeaderId = null;
                    _db.Groups.Update(group);
                    await _db.SaveChangesAsync();

                    _db.Groups.Remove(group);
                    await _db.SaveChangesAsync();
                }
            }

            // Finally remove the student
            var toDelete = await _db.Students.FindAsync(s.Id);
            if (toDelete == null) return NotFound();
            _db.Students.Remove(toDelete);
            await _db.SaveChangesAsync();
            return NoContent();
        }

    }
}
