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
    /// <summary>
    /// Kontroler do zarządzania powiadomieniami użytkowników.
    /// </summary>
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly wspolpracujmy.Services.NotificationService _notifications;
        /// <summary>
        /// Tworzy kontroler powiadomień z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public NotificationsController(AppDbContext db, wspolpracujmy.Services.NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        // [HttpGet]
        // Removed: returning all notifications globally. Use per-user notifications endpoints.
        // public async Task<IEnumerable<Notification>> Get() => await _db.Notifications.ToListAsync();

        [HttpGet("{id:int}")]
        /// <summary>
        /// Pobiera powiadomienie po identyfikatorze.
        /// </summary>
        /// <param name="id">Id powiadomienia.</param>
        /// <returns>Obiekt Notification lub NotFound.</returns>
        public async Task<ActionResult<Notification>> Get(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();
            return n;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetForUser([FromQuery] int? userId)
        {
            if (!userId.HasValue) return BadRequest("userId query parameter is required.");
            var list = await _notifications.GetNotificationsForUserAsync(userId.Value);
            return Ok(list);
        }

        [HttpPost]
        /// <summary>
        /// Tworzy nowe powiadomienie.
        /// </summary>
        /// <param name="notification">Obiekt powiadomienia do utworzenia.</param>
        /// <returns>Utworzone powiadomienie z kodem 201 Created.</returns>
        public async Task<ActionResult<Notification>> Post(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = notification.Id }, notification);
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return BadRequest("ids required");
            await _notifications.MarkAsReadAsync(ids);
            return NoContent();
        }
    }
}
