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
    public class MeetingTypesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public MeetingTypesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<MeetingType>> Get() => await _db.Meeting_types.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MeetingType>> Get(int id)
        {
            var m = await _db.Meeting_types.FindAsync(id);
            if (m == null) return NotFound();
            return m;
        }
    }
}
