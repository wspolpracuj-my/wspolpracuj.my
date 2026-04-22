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
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TagsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Tag>> Get() => await _db.Tags.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Tag>> Get(int id)
        {
            var t = await _db.Tags.FindAsync(id);
            if (t == null) return NotFound();
            return t;
        }
    }
}
