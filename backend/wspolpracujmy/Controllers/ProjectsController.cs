using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    /// Kontroler do zarządzania projektami i ich podsumowaniami.
    /// </summary>
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ProjectService _projectService;
        private readonly ProjectCommentService _projectCommentService;
        private readonly wspolpracujmy.Services.GroupAuthorizationService _groupAuth;
        private readonly NotificationService _notifications;

        /// <summary>
        /// Tworzy kontroler projektów z wymaganymi zależnościami.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        /// <param name="projectService">Serwis do pobierania podsumowań projektów.</param>
        /// <param name="projectCommentService">Serwis obsługi komentarzy projektów.</param>
        public ProjectsController(AppDbContext db, ProjectService projectService, ProjectCommentService projectCommentService, wspolpracujmy.Services.GroupAuthorizationService groupAuth, NotificationService notifications)
        {
            _db = db;
            _projectService = projectService;
            _projectCommentService = projectCommentService;
            _groupAuth = groupAuth;
            _notifications = notifications;
        }

        [HttpGet]
        /// <summary>
        /// Zwraca listę podsumowań wszystkich projektów.
        /// </summary>
        /// <returns>Lista podsumowań projektów.</returns>
        public async Task<ActionResult<IEnumerable<ProjectSummaryDto>>> Get()
        {
            var summaries = await _projectService.GetAllProjectSummariesAsync();
            return Ok(summaries);
        }

        // trzebazmienickoniecznie
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CompanyOnly")]
        /// <summary>
        /// Tworzy nowy projekt na podstawie danych DTO.
        /// </summary>
        /// <param name="dto">Dane potrzebne do utworzenia projektu.</param>
        /// <returns>Utworzony projekt z kodem 201 Created.</returns>
        public async Task<ActionResult<Project>> Post([FromBody] CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(err =>
                        string.IsNullOrEmpty(err.ErrorMessage) ? e.Key : err.ErrorMessage));
                return BadRequest(new { message = "Nieprawidłowe dane projektu.", errors = errors.ToList() });
            }

            // If caller is a Company, derive CompanyId from their user account; Admin may provide CompanyId in DTO.
            var role = wspolpracujmy.Services.GroupAuthorizationService.GetRoleFromClaims(User);
            var userIdMaybe = wspolpracujmy.Services.GroupAuthorizationService.GetUserIdFromClaims(User);
            if (role == "Company")
            {
                if (!userIdMaybe.HasValue) return Unauthorized();
                var userId = userIdMaybe.Value;
                var companyForUser = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (companyForUser == null) return Forbid();
                dto.CompanyId = companyForUser.Id;
            }

            var currentUserId = userIdMaybe ?? GetCurrentUserId();

            // load related entities
            var company = await _db.Companies.FindAsync(dto.CompanyId);
            if (company == null)
                return NotFound($"Firma o id {dto.CompanyId} nie została znaleziona.");

            if (company.UserId != currentUserId && !IsAdmin())
                return Forbid("No permission to create project for this company");

            var meetingType = await _db.Meeting_types.FindAsync(dto.MeetingTypeId);
            if (meetingType == null)
                return NotFound($"Typ spotkania o id {dto.MeetingTypeId} nie został znaleziony.");

            var project = new Project
            {
                CompanyId = dto.CompanyId,
                Topic = dto.Topic,
                Description = dto.Description,
                ProjectGoal = dto.ProjectGoal,
                WorkScope = dto.WorkScope,
                NeededTechnologies = dto.NeededTechnologies,
                MaxGroups = dto.MaxGroups,
                MaxNumberGroupMembers = dto.MaxNumberGroupMembers,
                MeetingTypeId = dto.MeetingTypeId,
                PartnershipType = dto.PartnershipType,
                LanguageDoc = dto.LanguageDoc,
                Notes = dto.Notes,
                Priority = dto.Priority,
                CreatedAt = System.DateTime.UtcNow,
                Company = company,
                MeetingType = meetingType
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDetails), new { id = project.Id }, project);
        }

        [HttpPut("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CompanyOnly")]
        /// <summary>
        /// Aktualizuje istniejący projekt na podstawie DTO.
        /// </summary>
        /// <param name="id">Id projektu do aktualizacji.</param>
        /// <param name="dto">Dane aktualizacyjne projektu.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> Put(int id, [FromBody] CreateProjectDto dto)
        {
            if (id <= 0) return BadRequest("Parametr id musi być większy niż 0.");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            // Authorization: companies can only update their own projects; admins can update any
            var role = wspolpracujmy.Services.GroupAuthorizationService.GetRoleFromClaims(User);
            var userIdMaybe = wspolpracujmy.Services.GroupAuthorizationService.GetUserIdFromClaims(User);
            if (role == "Company")
            {
                if (!userIdMaybe.HasValue) return Unauthorized();
                var userId = userIdMaybe.Value;
                var companyForUser = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (companyForUser == null) return Forbid();
                if (project.CompanyId != companyForUser.Id) return Forbid();
                // ensure dto.CompanyId stays with this company
                dto.CompanyId = companyForUser.Id;
            }
            // admins may provide dto.CompanyId to reassign project
            var company = await _db.Companies.FindAsync(dto.CompanyId);
            if (company == null)
                return NotFound($"Firma o id {dto.CompanyId} nie została znaleziona.");

            var meetingType = await _db.Meeting_types.FindAsync(dto.MeetingTypeId);
            if (meetingType == null)
                return NotFound($"Typ spotkania o id {dto.MeetingTypeId} nie został znaleziony.");

            // update fields
            project.CompanyId = dto.CompanyId;
            project.Topic = dto.Topic;
            project.Description = dto.Description;
            project.ProjectGoal = dto.ProjectGoal;
            project.WorkScope = dto.WorkScope;
            project.NeededTechnologies = dto.NeededTechnologies;
            project.MaxGroups = dto.MaxGroups;
            project.MaxNumberGroupMembers = dto.MaxNumberGroupMembers;
            project.MeetingTypeId = dto.MeetingTypeId;
            project.PartnershipType = dto.PartnershipType;
            project.LanguageDoc = dto.LanguageDoc;
            project.Notes = dto.Notes;
            project.Priority = dto.Priority;
            project.Company = company;
            project.MeetingType = meetingType;

            _db.Projects.Update(project);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CompanyOnly")]
        /// <summary>
        /// Usuwa projekt o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id projektu do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();

            var role = wspolpracujmy.Services.GroupAuthorizationService.GetRoleFromClaims(User);
            var userIdMaybe = wspolpracujmy.Services.GroupAuthorizationService.GetUserIdFromClaims(User);
            if (role == "Company")
            {
                if (!userIdMaybe.HasValue) return Unauthorized();
                var userId = userIdMaybe.Value;
                var companyForUser = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (companyForUser == null) return Forbid();
                if (p.CompanyId != companyForUser.Id) return Forbid();
            }

            _db.Projects.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("summary")]
        /// <summary>
        /// Zwraca podsumowania projektów dla konkretnej firmy.
        /// </summary>
        /// <param name="companyId">Id firmy, dla której pobieramy projekty.</param>
        /// <returns>Lista podsumowań projektów.</returns>
        public async Task<ActionResult<List<ProjectSummaryDto>>> GetSummary([FromQuery] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("Parametr companyId musi być podany i większy niż 0.");

            var summaries = await _projectService.GetProjectsForCompanyAsync(companyId);
            return Ok(summaries);
        }

        [HttpGet("summary/all")]
        /// <summary>
        /// Zwraca podsumowania wszystkich projektów (bez filtrowania).
        /// </summary>
        /// <returns>Lista podsumowań projektów.</returns>
        public async Task<ActionResult<List<ProjectSummaryDto>>> GetAllSummaries()
        {
            var summaries = await _projectService.GetAllProjectSummariesAsync();
            return Ok(summaries);
        }

        [HttpGet("{projectId:int}/groups")]
        /// <summary>
        /// Zwraca listę grup przypisanych do danego projektu.
        /// </summary>
        /// <param name="projectId">Id projektu.</param>
        /// <returns>Lista grup powiązanych z projektem.</returns>
        public async Task<ActionResult<List<Group>>> GetGroupsForProject(int projectId)
        {
            if (projectId <= 0)
                return BadRequest("Parametr projectId musi być podany i większy niż 0.");

            var exists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!exists) return NotFound();

            var groups = await _db.Groups
                .Where(g => g.ProjectId == projectId)
                .ToListAsync();

            return Ok(groups);
        }

        [HttpDelete("{projectId:int}/groups/{groupId:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CompanyOnly")]
        /// <summary>
        /// Usuwa powiązanie grupy z projektu (nie usuwa projektu ani grupy).
        /// </summary>
        /// <param name="projectId">Id projektu.</param>
        /// <param name="groupId">Id grupy do odpięcia od projektu.</param>
        /// <returns>Brak treści (204) lub odpowiedni kod błędu.</returns>
        public async Task<IActionResult> RemoveGroupFromProject(int projectId, int groupId)
        {
            if (projectId <= 0 || groupId <= 0)
                return BadRequest("Parametry projectId i groupId muszą być większe niż 0.");

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound($"Projekt o id {projectId} nie został znaleziony.");

            var role = wspolpracujmy.Services.GroupAuthorizationService.GetRoleFromClaims(User);
            var userIdMaybe = wspolpracujmy.Services.GroupAuthorizationService.GetUserIdFromClaims(User);
            if (role == "Company")
            {
                if (!userIdMaybe.HasValue) return Unauthorized();
                var companyForUser = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userIdMaybe.Value);
                if (companyForUser == null) return Forbid();
                if (project.CompanyId != companyForUser.Id) return Forbid();
            }

            var group = await _db.Groups.FindAsync(groupId);
            if (group == null) return NotFound($"Grupa o id {groupId} nie została znaleziona.");

            if (group.ProjectId != projectId)
                return BadRequest("Grupa nie jest przypisana do wskazanego projektu.");

            group.ProjectId = null;
            group.Project = null;

            _db.Groups.Update(group);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{projectId:int}/group-requests")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CompanyOnly")]
        /// <summary>
        /// Zwraca oczekujące zgłoszenia zespołów (ProjectRequest) do projektu.
        /// </summary>
        public async Task<ActionResult<List<ProjectGroupRequestDto>>> GetPendingGroupRequests(int projectId)
        {
            if (projectId <= 0)
                return BadRequest("Parametr projectId musi być podany i większy niż 0.");

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            if (!await CanCompanyManageProjectAsync(project))
                return Forbid();

            var pending = await _db.GroupRequests
                .Include(gr => gr.Group)
                    .ThenInclude(g => g.Members)
                .Where(gr => gr.ProjectId == projectId && gr.Status == GroupStatus.Pending)
                .ToListAsync();

            var requests = pending
                .Where(gr => string.Equals(gr.Type, "ProjectRequest", StringComparison.OrdinalIgnoreCase))
                .Select(gr => new ProjectGroupRequestDto
                {
                    RequestId = gr.Id,
                    GroupId = gr.GroupId,
                    GroupName = gr.Group.Name,
                    MemberCount = gr.Group.Members.Count,
                    CreatedAt = gr.CreatedAt
                })
                .ToList();

            return Ok(requests);
        }

        [HttpPost("{projectId:int}/group-requests/{requestId:int}/respond")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CompanyOnly")]
        /// <summary>
        /// Akceptuje lub odrzuca zgłoszenie zespołu do projektu.
        /// </summary>
        public async Task<IActionResult> RespondToGroupRequest(int projectId, int requestId, [FromBody] RespondGroupRequestDto dto)
        {
            if (projectId <= 0 || requestId <= 0)
                return BadRequest("Parametry projectId i requestId muszą być większe niż 0.");
            if (dto == null || string.IsNullOrWhiteSpace(dto.Action))
                return BadRequest("Pole action jest wymagane (accept lub decline).");

            var action = dto.Action.Trim().ToLowerInvariant();
            if (action != "accept" && action != "decline")
                return BadRequest("Akcja musi być 'accept' lub 'decline'.");

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound();

            if (!await CanCompanyManageProjectAsync(project))
                return Forbid();

            var req = await _db.GroupRequests
                .Include(r => r.Group)
                    .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ProjectId == projectId);

            if (req == null) return NotFound();
            if (req.Type == null || !req.Type.Equals("ProjectRequest", StringComparison.OrdinalIgnoreCase))
                return BadRequest("To nie jest zgłoszenie zespołu do projektu.");

            if (req.Status != GroupStatus.Pending)
                return BadRequest("Zgłoszenie zostało już rozpatrzone.");

            var group = req.Group;
            if (group == null) return NotFound("Powiązana grupa nie została znaleziona.");

            if (action == "accept")
            {
                if (project.MaxGroups.HasValue)
                {
                    var acceptedCount = await _db.Groups.CountAsync(g =>
                        g.ProjectId == projectId && g.IsAccepted == GroupStatus.Accepted);
                    if (acceptedCount >= project.MaxGroups.Value)
                        return BadRequest($"Projekt osiągnął maksymalną liczbę zespołów ({project.MaxGroups.Value}).");
                }

                var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                if (memberCount > project.MaxNumberGroupMembers)
                    return BadRequest($"Nie można zaakceptować zespołu: ma {memberCount} członków, limit projektu to {project.MaxNumberGroupMembers}.");

                if (group.ProjectId.HasValue && group.ProjectId.Value != projectId)
                    return BadRequest("Zespół jest już przypisany do innego projektu.");

                group.ProjectId = projectId;
                group.IsAccepted = GroupStatus.Accepted;
                req.Status = GroupStatus.Accepted;
            }
            else
            {
                req.Status = GroupStatus.Declined;
            }

            req.RespondedAt = DateTime.UtcNow;
            var responderId = GroupAuthorizationService.GetUserIdFromClaims(User);
            if (!responderId.HasValue) return Unauthorized();
            req.RespondedByUserId = responderId.Value;

            _db.Groups.Update(group);
            _db.GroupRequests.Update(req);
            await _db.SaveChangesAsync();

            try
            {
                var members = await _db.Students.Where(s => s.GroupId == group.Id).ToListAsync();
                var companyName = project.Company?.CompanyName ?? "Firma";
                var projectName = project.Topic;
                var accepted = action == "accept";
                var content = accepted
                    ? Notification.FormatCompanyProjectAccepted(companyName, projectName)
                    : Notification.FormatCompanyProjectDeclined(companyName, projectName);
                var linkTarget = Notification.LinkTargetProjectDecision(projectId, accepted);
                foreach (var member in members)
                {
                    await _notifications.CreateNotificationAsync(member.UserId, content, linkTarget);
                }
            }
            catch
            {
                // Powiadomienia nie są krytyczne dla akceptacji.
            }

            return Ok(new { requestId = req.Id, groupId = group.Id, status = req.Status.ToString() });
        }

        [HttpGet("{id:int}/details")]
        /// <summary>
        /// Zwraca szczegółowe informacje o projekcie.
        /// </summary>
        /// <param name="id">Id projektu.</param>
        /// <returns>DTO z detalami projektu lub NotFound.</returns>
        public async Task<ActionResult<ProjectDetailsDto>> GetDetails(int id)
        {
            if (id <= 0) return BadRequest("Parametr id musi być większy niż 0.");

            var dto = await _db.Projects
                .Where(p => p.Id == id)
                .Select(p => new ProjectDetailsDto
                {
                    Id = p.Id,
                    CompanyName = p.Company != null ? p.Company.CompanyName : string.Empty,
                    Topic = p.Topic,
                    Description = p.Description,
                    ProjectGoal = p.ProjectGoal,
                    WorkScope = p.WorkScope,
                    NeededTechnologies = p.NeededTechnologies,
                    CreatedAt = p.CreatedAt,
                    MaxGroups = p.MaxGroups,
                    MaxNumberGroupMembers = p.MaxNumberGroupMembers,
                    CurrentGroupsCount = p.Groups.Count(g => g.IsAccepted == GroupStatus.Accepted),
                    MeetingTypeId = p.MeetingTypeId,
                    MeetingTypeName = p.MeetingType != null ? p.MeetingType.Type : null,
                    PartnershipType = p.PartnershipType,
                    LanguageDoc = p.LanguageDoc,
                    Priority = p.Priority,
                    Notes = p.Notes,
                    Tags = p.ProjectTags.Select(pt => pt.Tag.Name).ToList()
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

        private async Task<bool> CanAccessProjectAsync(int projectId, int userId)
        {
            // Check if user is the company owner
            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;
            if (project.Company.UserId == userId) return true;

            // Check if user is a member of a group in the project
            var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.UserId == userId);
            if (student?.Group?.ProjectId == projectId) return true;

            return false;
        }

        private async Task<bool> CanManageCompanyAsync(int companyId, int userId)
        {
            var company = await _db.Companies.FindAsync(companyId);
            return company?.UserId == userId;
        }

        private async Task<bool> CanCompanyManageProjectAsync(Project project)
        {
            if (IsAdmin()) return true;

            var role = GroupAuthorizationService.GetRoleFromClaims(User);
            var isCompany = string.Equals(role, "Company", StringComparison.OrdinalIgnoreCase)
                || role == ((int)Models.Role.Company).ToString();
            if (!isCompany) return false;

            var userIdMaybe = GroupAuthorizationService.GetUserIdFromClaims(User);
            if (!userIdMaybe.HasValue) return false;

            var companyForUser = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userIdMaybe.Value);
            return companyForUser != null && project.CompanyId == companyForUser.Id;
        }
    }
}
