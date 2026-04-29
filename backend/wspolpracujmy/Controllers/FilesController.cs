using System;
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

        [HttpPost]
        /// <summary>
        /// Tworzy nowe metadane pliku w bazie.
        /// </summary>
        /// <param name="file">Obiekt metadanych pliku do zapisania.</param>
        /// <returns>Utworzony obiekt pliku z kodem 201 Created.</returns>
        public async Task<ActionResult<FileEntity>> Post(FileEntity file)
        {
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
            _db.Files.Remove(f);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
