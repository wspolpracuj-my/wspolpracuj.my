using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;

namespace wspolpracujmy.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _db;
        public NotificationService(AppDbContext db) => _db = db;

        public async Task<Notification> CreateNotificationAsync(int userId, string content, string? linkTarget = null, int? groupRequestId = null)
        {
            // Dedupe: avoid creating repeated identical notifications for the same user
            // within a short time window. This prevents spamming when events fire multiple
            // times in quick succession.
            var now = System.DateTime.UtcNow;
            var window = TimeSpan.FromMinutes(5);
            var cutoff = now - window;

            var existing = await _db.Notifications
                .Where(n => n.UserId == userId && n.Content == content && n.LinkTarget == linkTarget && n.GroupRequestId == groupRequestId && n.CreatedAt >= cutoff)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return existing;
            }

            var user = await _db.Users.FindAsync(userId);
            var n = new Notification
            {
                UserId = userId,
                User = user ?? null!,
                Content = content,
                Status = NotificationStatus.NotRead,
                CreatedAt = now,
                LinkTarget = linkTarget,
                GroupRequestId = groupRequestId
            };
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();
            return n;
        }

        public async Task<List<NotificationDto>> GetNotificationsForUserAsync(int userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    LinkTarget = n.LinkTarget
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return;
            n.Status = NotificationStatus.Read;
            await _db.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(System.Collections.Generic.IEnumerable<int> ids)
        {
            var list = await _db.Notifications.Where(n => ids.Contains(n.Id)).ToListAsync();
            if (list.Count == 0) return;
            foreach (var it in list) it.Status = NotificationStatus.Read;
            await _db.SaveChangesAsync();
        }

        public async Task MarkAsReadForUserAsync(int userId, System.Collections.Generic.IEnumerable<int> ids)
        {
            var list = await _db.Notifications
                .Where(n => ids.Contains(n.Id) && n.UserId == userId)
                .ToListAsync();
            if (list.Count == 0) return;
            foreach (var it in list) it.Status = NotificationStatus.Read;
            await _db.SaveChangesAsync();
        }
    }
}
