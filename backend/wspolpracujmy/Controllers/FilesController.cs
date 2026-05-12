using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do zarządzania metadanymi plików w aplikacji.
    /// </summary>
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy kontroler plików z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public FilesController(AppDbContext db) => _db = db;

        // [HttpGet]
        // Removed: returning all files. Use project-specific or owner-specific listing instead.
        // public async Task<IEnumerable<FileEntity>> Get() => await _db.Files.ToListAsync();

        [HttpGet("{id:guid}")]
        /// <summary>
        /// Pobiera metadane pliku po identyfikatorze GUID.
        /// </summary>
        /// <param name="id">Id pliku (GUID).</param>
        /// <returns>Obiekt metadanych pliku lub NotFound.</returns>
        public async Task<ActionResult<FileEntity>> Get(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) return NotFound();
            return f;
        }

        [HttpGet("group/{groupId}")]
        public async Task<ActionResult<IEnumerable<FileEntity>>> GetByGroupId(int groupId)
        {
            int currentUserId = GetCurrentUserId();
            var group = await _db.Groups.Include(g => g.Project).ThenInclude(p => p.Company).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();
            if (group.Project.Company.UserId != currentUserId && !await _db.Students.AnyAsync(s => s.GroupId == groupId && s.UserId == currentUserId) && !IsAdmin())
                return Forbid("No access to this group's files");

            var files = await _db.Files
                .Where(f => f.GroupId == groupId)
                .ToListAsync();
            return files;
        }

        [HttpPost]
        /// <summary>
        /// Tworzy nowe metadane pliku w bazie.
        /// </summary>
        /// <param name="file">Obiekt metadanych pliku do zapisania.</param>
        /// <returns>Utworzony obiekt pliku z kodem 201 Created.</returns>
        public async Task<ActionResult<FileEntity>> Post(FileEntity file)
        {
            int currentUserId = GetCurrentUserId();
            var group = await _db.Groups.Include(g => g.Project).ThenInclude(p => p.Company).FirstOrDefaultAsync(g => g.Id == file.GroupId);
            if (group == null) return NotFound("Group not found");
            if (group.Project.Company.UserId != currentUserId && !await _db.Students.AnyAsync(s => s.GroupId == file.GroupId && s.UserId == currentUserId) && !IsAdmin())
                return Forbid("No permission to upload file to this group");

            _db.Files.Add(file);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = file.Id }, file);
        }

        [HttpPut("{id:guid}")]
        /// <summary>
        /// Aktualizuje metadane pliku (zamienia cały obiekt).
        /// </summary>
        /// <param name="id">Id pliku (GUID).</param>
        /// <param name="file">Zaktualizowany obiekt pliku.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> Put(Guid id, FileEntity file)
        {
            if (id != file.Id) return BadRequest();

            int currentUserId = GetCurrentUserId();
            if (!await CanManageFileAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to update this file");

            _db.Entry(file).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        /// <summary>
        /// Usuwa metadane pliku po identyfikatorze.
        /// </summary>
        /// <param name="id">Id pliku (GUID) do usunięcia.</param>
        /// <returns>Brak treści (204) gdy usunięto, lub NotFound.</returns>
        public async Task<IActionResult> Delete(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (!await CanManageFileAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to delete this file");

            _db.Files.Remove(f);
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

        private async Task<bool> CanManageFileAsync(Guid fileId, int userId)
        {
            var groupFile = await _db.GroupFiles.Include(gf => gf.Group).ThenInclude(g => g.Project).ThenInclude(p => p.Company).FirstOrDefaultAsync(gf => gf.FileId == fileId);
            if (groupFile == null) return false;

            var group = groupFile.Group;

            // Company owner can manage files in their projects
            if (group.Project?.Company?.UserId == userId) return true;

            // Group members can manage files in their group
            var isMember = await _db.Students.AnyAsync(s => s.GroupId == group.Id && s.UserId == userId);
            return isMember;
        }
    }
}
