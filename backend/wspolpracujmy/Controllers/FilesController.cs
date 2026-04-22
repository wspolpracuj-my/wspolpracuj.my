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
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public FilesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<FileEntity>> Get() => await _db.Files.ToListAsync();

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FileEntity>> Get(Guid id)
        {
            var f = await _db.Files.FindAsync(id);
            if (f == null) return NotFound();
            return f;
        }

        [HttpPost]
        public async Task<ActionResult<FileEntity>> Post(FileEntity file)
        {
            _db.Files.Add(file);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = file.Id }, file);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put(Guid id, FileEntity file)
        {
            if (id != file.Id) return BadRequest();
            _db.Entry(file).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
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
