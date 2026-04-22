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
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public StudentsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Student>> Get() => await _db.Students.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Student>> Get(int id)
        {
            var s = await _db.Students.FindAsync(id);
            if (s == null) return NotFound();
            return s;
        }

        [HttpPost]
        public async Task<ActionResult<Student>> Post(Student student)
        {
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = student.Id }, student);
        }

        public class ChangeStudentGroupDto { public int GroupId { get; set; } }

        [HttpPatch("{id:int}/group")]
        public async Task<IActionResult> PatchGroup(int id, [FromBody] ChangeStudentGroupDto dto)
        {
            if (dto == null) return BadRequest();

            var student = await _db.Students.FindAsync(id);
            if (student == null) return NotFound();

            var group = await _db.Groups.FindAsync(dto.GroupId);
            if (group == null) return BadRequest(new { error = "Group not found" });

            student.GroupId = dto.GroupId;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _db.Students.FindAsync(id);
            if (s == null) return NotFound();
            _db.Students.Remove(s);
            await _db.SaveChangesAsync();
            return NoContent();
        }
        
    }
}
