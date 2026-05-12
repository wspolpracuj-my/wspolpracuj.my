using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Kontroler do zarządzania grupami studentów powiązanymi z projektami.
    /// </summary>
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy kontroler grup z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public GroupsController(AppDbContext db) => _db = db;

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
        /// <summary>
        /// Tworzy nową grupę w bazie danych.
        /// </summary>
        /// <param name="group">Obiekt grupy do utworzenia.</param>
        /// <returns>Utworzona grupa z kodem 201 Created.</returns>
        public async Task<ActionResult<Group>> Post(Group group)
        {
            int currentUserId = GetCurrentUserId();
            // Check if user can access the project
            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == group.ProjectId);
            if (project == null) return NotFound("Project not found");
            if (project.Company.UserId != currentUserId && !IsAdmin())
                return Forbid("No permission to create group for this project");

            _db.Groups.Add(group);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = group.Id }, group);
        }

        [HttpPatch("{id:int}")]
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

            int currentUserId = GetCurrentUserId();
            if (!await CanManageGroupAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to update this group");

            patch.ApplyTo(group, ModelState);
            if (!ModelState.IsValid) return BadRequest(ModelState);

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
        /// <summary>
        /// Usuwa grupę o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id grupy do usunięcia.</param>
        /// <returns>Brak treści (204) gdy usunięto, lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _db.Groups.FindAsync(id);
            if (g == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (!await CanManageGroupAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to delete this group");

            _db.Groups.Remove(g);
            await _db.SaveChangesAsync();
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
            if (projectId <= 0) return BadRequest("projectId must be greater than 0");

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
                    Members = g.Members.Select(m => new MemberSummaryDto
                    {
                        Id = m.Id,
                        UserName = m.User.Name + " " + m.User.Surname
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
            if (id <= 0) return BadRequest("id must be greater than 0");

            var dto = await _db.Groups
                .Where(g => g.Id == id)
                .Include(g => g.Project)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Select(g => new GroupSummaryDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Members = g.Members.Select(m => new MemberSummaryDto
                    {
                        Id = m.Id,
                        UserName = m.User.Name + " " + m.User.Surname
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
            var group = await _db.Groups.Include(g => g.Project).ThenInclude(p => p.Company).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return false;

            // Company owner can manage groups in their projects
            if (group.Project.Company.UserId == userId) return true;

            // Group leader can manage their group
            if (group.LeaderId == userId) return true;

            // Group members can view/manage their group (for some actions)
            var isMember = await _db.Students.AnyAsync(s => s.GroupId == groupId && s.UserId == userId);
            return isMember;
        }
    }
}
