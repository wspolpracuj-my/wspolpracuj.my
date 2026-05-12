using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using wspolpracujmy.Data;
using Microsoft.EntityFrameworkCore;

namespace wspolpracujmy.Services.Authorization
{
    public class GroupOwnerHandler : AuthorizationHandler<GroupOwnerRequirement>
    {
        private readonly AppDbContext _db;

        public GroupOwnerHandler(AppDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupOwnerRequirement requirement)
        {
            // Extract HttpContext from resource (works for MVC)
            var httpContext = (context.Resource as AuthorizationFilterContext)?.HttpContext;
            if (httpContext == null)
            {
                // fallback: try direct cast
                httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            }

            if (httpContext == null)
            {
                // cannot evaluate, do not succeed
                return;
            }

            // Extract user id and role from claims (robust against different claim names)
            var user = context.User;
            string? userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? user.FindFirst("id")?.Value
                                ?? user.FindFirst("sub")?.Value;
            string? role = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return; // unauthenticated
            }

            if (role == null || role != "Student")
            {
                // role must be Student
                return;
            }

            // Get group id from route values - common keys: "id", "groupId"
            var route = httpContext.Request.RouteValues;
            object? rv = null;
            if (!route.TryGetValue("id", out rv)) route.TryGetValue("groupId", out rv);
            if (rv == null) return;

            if (!int.TryParse(rv.ToString(), out var groupId)) return;

            // Find student record for this user
            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return;

            // Load group and check leader
            var group = await _db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return;

            if (group.LeaderId.HasValue && group.LeaderId.Value == student.Id)
            {
                context.Succeed(requirement);
            }
        }
    }
}
