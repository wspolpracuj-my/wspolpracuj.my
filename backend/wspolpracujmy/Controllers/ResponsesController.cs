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
    public class ResponsesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ResponsesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Response>> Get() => await _db.Responses.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Response>> Get(int id)
        {
            var r = await _db.Responses.FindAsync(id);
            if (r == null) return NotFound();
            return r;
        }

        [HttpPost]
        public async Task<ActionResult<Response>> Post(Response response)
        {
            _db.Responses.Add(response);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Responses.FindAsync(id);
            if (r == null) return NotFound();
            _db.Responses.Remove(r);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
