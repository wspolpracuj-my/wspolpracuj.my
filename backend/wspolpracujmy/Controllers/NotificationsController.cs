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
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public NotificationsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Notification>> Get() => await _db.Notifications.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Notification>> Get(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();
            return n;
        }

        [HttpPost]
        public async Task<ActionResult<Notification>> Post(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = notification.Id }, notification);
        }
    }
}
