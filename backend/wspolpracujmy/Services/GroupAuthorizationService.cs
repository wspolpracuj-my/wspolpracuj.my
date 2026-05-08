using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;

namespace wspolpracujmy.Services
{
    // Optional service for explicit ownership checks from controllers/services
    public class GroupAuthorizationService
    {
        private readonly AppDbContext _db;

        public GroupAuthorizationService(AppDbContext db)
        {
            _db = db;
        }

        // Extracts user id from ClaimsPrincipal robustly
        public static int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? user.FindFirst("id")?.Value
                            ?? user.FindFirst("sub")?.Value;
            if (int.TryParse(userIdStr, out var id)) return id;
            return null;
        }

        // Extract role
        public static string? GetRoleFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
        }

        // Checks if the current user (by userId) is the leader of the group
        public async Task<bool> IsUserLeaderOfGroupAsync(int userId, int groupId)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return false;
            var group = await _db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return false;
            return group.LeaderId.HasValue && group.LeaderId.Value == student.Id;
        }
    }
}
