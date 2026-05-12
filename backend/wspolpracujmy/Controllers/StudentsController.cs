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

        [HttpGet("{id:int}")]
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
        public class ChangeStudentGroupDto { public int? GroupId { get; set; } }

        [HttpPatch("{id:int}/group")]
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

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        /// <summary>
        /// Usuwa studenta o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id studenta do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Students.FindAsync(id);
            if (s == null) return NotFound();
            _db.Students.Remove(s);
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
