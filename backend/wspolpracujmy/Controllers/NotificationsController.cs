using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        [Microsoft.AspNetCore.Authorization.Authorize]
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
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetForUser([FromQuery] int? userId)
        {
            var currentUserId = GetCurrentUserId();
            var targetUserId = userId ?? currentUserId;
            if (userId.HasValue && targetUserId != currentUserId && !IsAdmin())
                return Forbid("Brak uprawnień do przeglądania powiadomień innego użytkownika.");

            var list = await _notifications.GetNotificationsForUserAsync(targetUserId);
            return Ok(list);
        }

        [HttpGet("all")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Zwraca wszystkie powiadomienia wszystkich użytkowników — tylko dla administratora.
        /// </summary>
        public async Task<ActionResult<IEnumerable<DTOs.AdminNotificationDto>>> GetAllNotifications()
        {
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            if (currentUser.Role != Models.Role.Admin) return Forbid();

            var list = await _db.Notifications
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new DTOs.AdminNotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    UserName = n.User != null ? n.User.Name + " " + n.User.Surname : null,
                    Content = n.Content,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    LinkTarget = n.LinkTarget,
                    GroupRequestId = n.GroupRequestId
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Tworzy nowe powiadomienie.
        /// </summary>
        /// <param name="notification">Obiekt powiadomienia do utworzenia.</param>
        /// <returns>Utworzone powiadomienie z kodem 201 Created.</returns>
        public async Task<ActionResult<Notification>> Post(Notification notification)
        {
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var role = User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;
            // Non-admins should not create notifications for other users (prevent impersonation)
            if (role != "Admin")
            {
                notification.UserId = currentUserId;
            }

            // Use NotificationService to create the notification (handles dedupe and FK)
            var created = await _notifications.CreateNotificationAsync(notification.UserId, notification.Content, notification.LinkTarget, notification.GroupRequestId);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPost("mark-read")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> MarkRead([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return BadRequest("Tablica 'ids' jest wymagana.");

            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized("Nieautoryzowany użytkownik.");

            await _notifications.MarkAsReadForUserAsync(currentUserId, ids);
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(claim.Value);
        }

        private bool IsAdmin()
        {
            var roleClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.Role);
            return roleClaim?.Value == "Admin";
        }
    }
}
