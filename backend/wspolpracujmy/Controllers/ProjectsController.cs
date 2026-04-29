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
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProjectsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Project>> Get() => await _db.Projects.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Project>> Get(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return NotFound();
            return p;
        }

        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<IEnumerable<Project>>> GetByCompanyId(int companyId)
        {
            var projects = await _db.Projects
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();
            return projects;
        }

        [HttpPost]
        public async Task<ActionResult<Project>> Post(Project project)
        {
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, Project project)
        {
            if (id != project.Id) return BadRequest();
            _db.Entry(project).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return NotFound();
            _db.Projects.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
