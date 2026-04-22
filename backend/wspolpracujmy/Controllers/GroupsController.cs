using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public GroupsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<object>> Get()
        {
            // return groups with computed member count
            return await _db.Groups
                .Select(g => new { g.Id, g.Name, g.ProjectId, g.IsAccepted, g.LeaderId, MemberCount = g.Members.Count })
                .ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Group>> Get(int id)
        {
            var g = await _db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g == null) return NotFound();
            return g;
        }

        [HttpPost]
        public async Task<ActionResult<Group>> Post(Group group)
        {
            _db.Groups.Add(group);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = group.Id }, group);
        }

        [HttpPatch("{id:int}")]
        [Consumes("application/json-patch+json")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<Group> patch)
        {
            if (patch == null) return BadRequest();

            var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();

            patch.ApplyTo(group, ModelState);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // opcjonalnie: walidacja/autoryzacja tutaj

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _db.Groups.FindAsync(id);
            if (g == null) return NotFound();
            _db.Groups.Remove(g);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
