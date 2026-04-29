using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;
using wspolpracujmy.Services;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Kontroler do zarządzania projektami, podsumowaniami i powiązanymi zasobami.
    /// </summary>
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ProjectService _projectService;
        private readonly ProjectCommentService _projectCommentService;

        /// <summary>
        /// Tworzy kontroler projektów z wymaganymi zależnościami.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        /// <param name="projectService">Serwis do pobierania podsumowań projektów.</param>
        /// <param name="projectCommentService">Serwis obsługi komentarzy projektów.</param>
        public ProjectsController(AppDbContext db, ProjectService projectService, ProjectCommentService projectCommentService)
        {
            _db = db;
            _projectService = projectService;
            _projectCommentService = projectCommentService;
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
        /// <summary>
        /// Tworzy nowy projekt na podstawie danych DTO.
        /// </summary>
        /// <param name="dto">Dane potrzebne do utworzenia projektu.</param>
        /// <returns>Utworzony projekt z kodem 201 Created.</returns>
        public async Task<ActionResult<Project>> Post([FromBody] Models.CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // load related entities
            var company = await _db.Companies.FindAsync(dto.CompanyId);
            if (company == null)
                return NotFound($"Company with id {dto.CompanyId} not found.");

            var meetingType = await _db.Meeting_types.FindAsync(dto.MeetingTypeId);
            if (meetingType == null)
                return NotFound($"MeetingType with id {dto.MeetingTypeId} not found.");

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
        /// <summary>
        /// Aktualizuje istniejący projekt na podstawie DTO.
        /// </summary>
        /// <param name="id">Id projektu do aktualizacji.</param>
        /// <param name="dto">Dane aktualizacyjne projektu.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> Put(int id, [FromBody] Models.CreateProjectDto dto)
        {
            if (id <= 0) return BadRequest("id must be greater than 0");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var project = await _db.Projects.FindAsync(id);
            if (project == null) return NotFound();

            // load related entities
            var company = await _db.Companies.FindAsync(dto.CompanyId);
            if (company == null)
                return NotFound($"Company with id {dto.CompanyId} not found.");

            var meetingType = await _db.Meeting_types.FindAsync(dto.MeetingTypeId);
            if (meetingType == null)
                return NotFound($"MeetingType with id {dto.MeetingTypeId} not found.");

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
        /// <summary>
        /// Usuwa projekt o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id projektu do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return NotFound();
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
                return BadRequest("companyId must be provided and greater than 0");

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
                return BadRequest("projectId must be provided and greater than 0");

            var exists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!exists) return NotFound();

            var groups = await _db.Groups
                .Where(g => g.ProjectId == projectId)
                .ToListAsync();

            return Ok(groups);
        }

        [HttpGet("{id:int}/details")]
        /// <summary>
        /// Zwraca szczegółowe informacje o projekcie.
        /// </summary>
        /// <param name="id">Id projektu.</param>
        /// <returns>DTO z detalami projektu lub NotFound.</returns>
        public async Task<ActionResult<ProjectDetailsDto>> GetDetails(int id)
        {
            if (id <= 0) return BadRequest("id must be greater than 0");

            var dto = await _db.Projects
                .Where(p => p.Id == id)
                .Select(p => new ProjectDetailsDto
                {
                    Id = p.Id,
                    Topic = p.Topic,
                    ProjectGoal = p.ProjectGoal,
                    WorkScope = p.WorkScope,
                    NeededTechnologies = p.NeededTechnologies,
                    MaxGroups = p.MaxGroups,
                    MaxNumberGroupMembers = p.MaxNumberGroupMembers,
                    LanguageDoc = p.LanguageDoc,
                    Priority = p.Priority,
                    Tags = p.ProjectTags.Select(pt => pt.Tag.Name).ToList()
                })
                .FirstOrDefaultAsync();

            if (dto == null) return NotFound();
            return Ok(dto);
        }
    }
}
