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
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CommentsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Comment>> Get() => await _db.Comments.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Comment>> Get(int id)
        {
            var c = await _db.Comments.FindAsync(id);
            if (c == null) return NotFound();
            return c;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<Comment>>> GetByProjectId(int projectId)
        {
            var comments = await _db.Comments
                .Include(c => c.User)
                .Where(c => c.ProjectId == projectId)
                .ToListAsync();
            return comments;
        }

        [HttpPost]
        public async Task<ActionResult<Comment>> Post(Comment comment)
        {
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = comment.Id }, comment);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Comments.FindAsync(id);
            if (c == null) return NotFound();
            _db.Comments.Remove(c);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
