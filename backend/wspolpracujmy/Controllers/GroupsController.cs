using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;
using wspolpracujmy.Services;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do zarządzania grupami studentów i ich członkami.
    /// </summary>
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifications;
        /// <summary>
        /// Tworzy kontroler grup z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public GroupsController(AppDbContext db, NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpGet]
        /// <summary>
        /// Zwraca listę grup z liczbą członków (skrótowe dane).
        /// </summary>
        /// <returns>Enumerowalna kolekcja obiektów z podsumowaniem grup.</returns>
        public async Task<IEnumerable<object>> Get()
        {
            // return groups with computed member count
            return await _db.Groups
                .Select(g => new { g.Id, g.Name, g.ProjectId, g.IsAccepted, g.LeaderId, MemberCount = g.Members.Count })
                .ToListAsync();
        }

        [HttpGet("{id:int}")]
        /// <summary>
        /// Pobiera grupę po jej identyfikatorze, włącznie z członkami.
        /// </summary>
        /// <param name="id">Id grupy.</param>
        /// <returns>Obiekt grupy lub NotFound jeśli nie istnieje.</returns>
        public async Task<ActionResult<Group>> Get(int id)
        {
            var g = await _db.Groups.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == id);
            if (g == null) return NotFound();
            return g;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<Group>>> GetByProjectId(int projectId)
        {
            var groups = await _db.Groups
                .Include(g => g.Members)
                .Where(g => g.ProjectId == projectId)
                .ToListAsync();
            return groups;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "StudentOnly")]
        /// <summary>
        /// Tworzy nową grupę w bazie danych.
        /// </summary>
        /// <param name="dto">DTO zawierające dane grupy do utworzenia.</param>
        /// <returns>Utworzona grupa z kodem 201 Created.</returns>
        public async Task<ActionResult<CreateGroupResultDto>> Post([FromBody] CreateGroupDto dto)
        {
            if (dto == null) return BadRequest();
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Nazwa jest wymagana.");

            // Pobierz aktualnego użytkownika z tokena
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            // Determine the leader from the authenticated user to avoid spoofing leaderId in DTO
            var currentStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (currentStudent == null) return Forbid();

            // Student może być liderem tylko jednej grupy i nie może już należeć do grupy
            var alreadyLeader = await _db.Groups.AnyAsync(g => g.LeaderId == currentStudent.Id);
            if (alreadyLeader) return BadRequest("Masz już przypisaną grupę jako lider.");

            if (currentStudent.GroupId.HasValue) return BadRequest("Masz już przypisaną grupę.");

            // Unikalność nazwy (case-insensitive)
            var normalized = dto.Name.Trim().ToLower();
            var nameTaken = await _db.Groups.AnyAsync(g => g.Name.ToLower() == normalized);
            if (nameTaken) return BadRequest("Nazwa grupy jest już zajęta.");

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var group = new Group
            {
                Name = dto.Name.Trim(),
                LeaderId = currentStudent.Id,
                Members = new List<Student>(),
                GroupRequests = new List<GroupRequest>(),
                GroupFiles = new List<GroupFile>(),
                CalendarEvents = new List<CalendarEvent>(),
                MaxMembers = dto.MaxMembers
            };

            _db.Groups.Add(group);
            await _db.SaveChangesAsync();

            currentStudent.GroupId = group.Id;
            currentStudent.Group = group;
            group.Leader = currentStudent;
            _db.Students.Update(currentStudent);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new CreateGroupResultDto
            {
                Id = group.Id,
                Name = group.Name,
                ProjectId = group.ProjectId,
                LeaderId = group.LeaderId,
                MemberCount = 1
            };

            return CreatedAtAction(nameof(Get), new { id = group.Id }, response);
        }

        [HttpPatch("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "GroupOwner")]
        [Consumes("application/json-patch+json")]
        /// <summary>
        /// Aktualizuje częściowo istniejącą grupę przy pomocy JSON Patch.
        /// </summary>
        /// <param name="id">Id grupy do zaktualizowania.</param>
        /// <param name="patch">Dokument JSON Patch opisujący zmiany.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<Group> patch)
        {
            if (patch == null) return BadRequest();

            var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();

            // get current user and student
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var currentStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (currentStudent == null) return Forbid();

            if (!group.LeaderId.HasValue || group.LeaderId.Value != currentStudent.Id)
                return Forbid();

            // Stosujemy patch na obiekcie w pamięci, następnie walidujemy konkretne reguły
            patch.ApplyTo(group, ModelState);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Walidacja unikalności nazwy (exclude current group)
            if (!string.IsNullOrWhiteSpace(group.Name))
            {
                var normalized = group.Name.Trim().ToLower();
                var conflict = await _db.Groups.AnyAsync(g => g.Id != id && g.Name.ToLower() == normalized);
                if (conflict) return BadRequest("Nazwa grupy jest już zajęta.");
            }

            // Walidacja MaxMembers: nie można ustawić poniżej aktualnej liczby członków
            if (group.MaxMembers.HasValue)
            {
                var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                if (group.MaxMembers.Value < memberCount)
                    return BadRequest("Nowy limit członków nie może być mniejszy niż aktualna liczba członków.");
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id:int}")]
        /// <summary>
        /// Aktualizuje istniejącą grupę - całość danych.
        /// </summary>
        /// <param name="id">Id grupy do zaktualizowania.</param>
        /// <param name="updateData">Nowe dane grupy.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> Put(int id, [FromBody] dynamic updateData)
        {
            var group = await _db.Groups.FindAsync(id);
            if (group == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (!await CanManageGroupAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to update this group");

            try
            {
                if (updateData.name != null)
                    group.Name = updateData.name;

                if (updateData.projectId != null)
                    group.ProjectId = updateData.projectId;

                if (updateData.isAccepted != null)
                    group.IsAccepted = updateData.isAccepted;

                if (updateData.leaderId != null)
                    group.LeaderId = updateData.leaderId;

                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Usuwa grupę o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id grupy do usunięcia.</param>
        /// <returns>Brak treści (204) gdy usunięto, lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _db.Groups.FindAsync(id);
            if (g == null) return NotFound();

            // get current user id from claims
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var isAdmin = currentUser.Role == Models.Role.Admin;

            var currentStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            var isLeader = currentStudent != null && g.LeaderId.HasValue && currentStudent.Id == g.LeaderId.Value;

            if (!isAdmin && !isLeader)
                return Forbid();

            _db.Groups.Remove(g);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{groupId:int}/members/{studentId:int}")]
        [Authorize]
        /// <summary>
        /// Usuwa studenta z grupy. Tylko lider grupy lub administrator mogą wykonać tę akcję.
        /// </summary>
        /// <param name="groupId">Id grupy.</param>
        /// <param name="studentId">Id studenta do usunięcia z grupy.</param>
        /// <returns>Brak treści (204) lub odpowiedni kod błędu.</returns>
        public async Task<IActionResult> RemoveMemberFromGroup(int groupId, int studentId)
        {
            if (groupId <= 0 || studentId <= 0)
                return BadRequest("Parametry groupId i studentId muszą być większe niż 0.");

            var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound($"Grupa o id {groupId} nie została znaleziona.");

            // get current user id from claims
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var isAdmin = currentUser.Role == Models.Role.Admin;

            var currentStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            var isLeader = currentStudent != null && group.LeaderId.HasValue && currentStudent.Id == group.LeaderId.Value;

            if (!isAdmin && !isLeader)
                return Forbid();

            var targetStudent = await _db.Students.FindAsync(studentId);
            if (targetStudent == null) return NotFound($"Student o id {studentId} nie został znaleziony.");

            if (!targetStudent.GroupId.HasValue || targetStudent.GroupId.Value != groupId)
                return BadRequest("Student nie jest członkiem tej grupy.");

            // If removing the leader, disallow removal entirely — group must be deleted instead.
            if (group.LeaderId.HasValue && group.LeaderId.Value == targetStudent.Id)
            {
                return BadRequest("Nie można usunąć lidera grupy; usuń grupę, aby usunąć lidera.");
            }

            targetStudent.GroupId = null;

            _db.Students.Update(targetStudent);
            _db.Groups.Update(group);
            await _db.SaveChangesAsync();

            try
            {
                var content = $"Zostałeś usunięty z grupy {group.Name}.";
                await _notifications.CreateNotificationAsync(targetStudent.UserId, content, $"group:{group.Id}");
            }
            catch
            {
                // ignore notification errors
            }

            return NoContent();
        }

        [HttpGet("project/{projectId:int}/summary")]
        /// <summary>
        /// Zwraca podsumowania grup (z członkami) dla zadanego projektu.
        /// </summary>
        /// <param name="projectId">Identyfikator projektu.</param>
        /// <returns>Lista podsumowań grup dla projektu.</returns>
        public async Task<ActionResult<List<GroupSummaryDto>>> GetByProjectSummary(int projectId)
        {
            if (projectId <= 0) return BadRequest("Parametr projectId musi być większy niż 0.");

            var exists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!exists) return NotFound();

            var summaries = await _db.Groups
                .Where(g => g.ProjectId == projectId)
                .Include(g => g.Project)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Select(g => new GroupSummaryDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    LeaderId = g.LeaderId ?? 0,
                    MemberCount = g.Members.Count,
                    Members = g.Members.Select(m => new MemberSummaryDto
                    {
                        Id = m.Id,
                        UserName = m.User.Name + " " + m.User.Surname,
                    }).ToList()
                })
                .ToListAsync();

            return Ok(summaries);
        }

        [HttpGet("{id:int}/summary")]
        /// <summary>
        /// Zwraca podsumowanie konkretnej grupy wraz z listą członków.
        /// </summary>
        /// <param name="id">Id grupy.</param>
        /// <returns>Podsumowanie grupy lub NotFound.</returns>
        public async Task<ActionResult<GroupSummaryDto>> GetSummary(int id)
        {
            if (id <= 0) return BadRequest("Parametr id musi być większy niż 0.");

            var dto = await _db.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Project)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Select(g => new GroupSummaryDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    LeaderId = g.LeaderId ?? 0,
                    MemberCount = g.Members.Count,
                    Members = g.Members.Select(m => new MemberSummaryDto
                    {
                        Id = m.Id,
                        UserName = m.User.Name + " " + m.User.Surname,
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (dto == null) return NotFound();
            return Ok(dto);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(claim.Value);
        }

        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
            return roleClaim?.Value == "Admin";
        }

        private async Task<bool> CanManageGroupAsync(int groupId, int userId)
        {
            var group = await _db.Groups.Include(g => g.Project).ThenInclude(p => p!.Company).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return false;

            // Company owner can manage groups in their projects
            if (group.Project?.Company?.UserId == userId) return true;

            // Group leader can manage their group
            if (group.LeaderId == userId) return true;

            // Group members can view/manage their group (for some actions)
            var isMember = await _db.Students.AnyAsync(s => s.GroupId == groupId && s.UserId == userId);
            return isMember;
        }
    }
}
