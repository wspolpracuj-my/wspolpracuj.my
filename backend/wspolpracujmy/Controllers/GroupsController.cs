using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        private readonly TeamCleanupService _teamCleanup;
        /// <summary>
        /// Tworzy kontroler grup z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public GroupsController(AppDbContext db, NotificationService notifications, TeamCleanupService teamCleanup)
        {
            _db = db;
            _notifications = notifications;
            _teamCleanup = teamCleanup;
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
            if (g == null) return NotFound(new { error = "Grupa nie została znaleziona." });
            return g;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<IEnumerable<Group>>> GetByProjectId(int projectId)
        {
            if (projectId <= 0) return BadRequest(new { error = "Nieprawidłowy numer projektu." });

            var project = await _db.Projects.Include(p => p.Company).FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return NotFound(new { error = "Projekt nie został znaleziony." });

            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            if (currentUser.Role != Models.Role.Admin)
            {
                // company must be owner of project
                if (currentUser.Role != Models.Role.Company) return StatusCode(403, new { error = "Nie masz uprawnień do wyświetlania tej strony." });
                if (project.Company == null || project.Company.UserId != currentUserId) return StatusCode(403, new { error = "Nie masz dostępu do tego projektu." });
            }

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
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { error = "Nazwa grupy jest wymagana." });

            // Pobierz aktualnego użytkownika z tokena
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            // Determine the leader from the authenticated user to avoid spoofing leaderId in DTO
            var currentStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (currentStudent == null) return StatusCode(403, new { error = "Nie jesteś studentem." });

            // Student może być liderem tylko jednej grupy i nie może już należeć do grupy
            var alreadyLeader = await _db.Groups.AnyAsync(g => g.LeaderId == currentStudent.Id);
            if (alreadyLeader) return BadRequest(new { error = "Już prowadzisz jedną grupę." });

            if (currentStudent.GroupId.HasValue) return BadRequest(new { error = "Już jesteś członkiem grupy." });

            // Unikalność nazwy (case-insensitive)
            var normalized = dto.Name.Trim().ToLower();
            var nameTaken = await _db.Groups.AnyAsync(g => g.Name.ToLower() == normalized);
            if (nameTaken) return BadRequest(new { error = "Ta nazwa grupy jest już zajęta. Wybierz inną." });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var group = new Group
            {
                Name = dto.Name.Trim(),
                LeaderId = currentStudent.Id,
                IsAccepted = GroupStatus.Pending,
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
            if (patch == null) return BadRequest(new { error = "Żądanie jest puste." });

            var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound(new { error = "Grupa nie została znaleziona." });

            // get current user and student
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            var currentStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (currentStudent == null) return StatusCode(403, new { error = "Nie jesteś studentem." });

            if (!group.LeaderId.HasValue || group.LeaderId.Value != currentStudent.Id)
                return StatusCode(403, new { error = "Nie masz uprawnień do edycji tej grupy." });

            // Stosujemy patch na obiekcie w pamięci, następnie walidujemy konkretne reguły
            patch.ApplyTo(group, ModelState);
            if (!ModelState.IsValid) return BadRequest(new { error = "Nieprawidłowe dane. Sprawdź pola i spróbuj ponownie." });

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
            if (updateData == null) return BadRequest("Dane do aktualizacji nie mogą być puste.");

            var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();

            int currentUserId = GetCurrentUserId();
            if (!await CanManageGroupAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to update this group");

            try
            {
                if (updateData.name != null)
                    group.Name = updateData.name;

                if (updateData.projectId != null)
                {
                    int projectId = (int)updateData.projectId;
                    group.ProjectId = projectId > 0 ? projectId : null;
                }

                if (updateData.isAccepted != null)
                    group.IsAccepted = updateData.isAccepted;

                if (updateData.maxMembers != null)
                {
                    int maxMembers = (int)updateData.maxMembers;
                    var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
                    if (maxMembers < memberCount)
                        return BadRequest("Nowy limit członków nie może być mniejszy niż aktualna liczba członków.");
                    group.MaxMembers = maxMembers;
                }

                if (updateData.leaderId != null)
                {
                    int leaderId = (int)updateData.leaderId;
                    group.LeaderId = leaderId > 0 ? leaderId : null;
                }

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

            await _teamCleanup.DeleteTeamAndCleanupFilesAsync(id);
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

        public class InviteMemberDto
        {
            [JsonProperty("email")]
            public string Email { get; set; } = string.Empty;
        }

        public class GroupInvitationDto
        {
            public int RequestId { get; set; }
            public int GroupId { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public int? MaxMembers { get; set; }
            public int MemberCount { get; set; }
        }

        [HttpPost("{groupId:int}/invite")]
        [Authorize(Policy = "StudentOnly")]
        /// <summary>
        /// Lider zaprasza studenta e-mailem (dozwolone domeny uczelniane).
        /// </summary>
        public async Task<ActionResult<GroupRequest>> InviteMember(int groupId, [FromBody] InviteMemberDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Adres e-mail jest wymagany." });

            if (!Group.IsAllowedStudentEmail(dto.Email))
                return BadRequest(new { message = "Można zapraszać wyłącznie studentów z domen @g.elearn.uz.zgora.pl lub @stud.uz.zgora.pl." });

            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var leaderStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (leaderStudent == null) return Forbid();

            var group = await _db.Groups.Include(g => g.Members).Include(g => g.Project).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound(new { message = $"Grupa o id {groupId} nie została znaleziona." });

            if (!group.LeaderId.HasValue || group.LeaderId.Value != leaderStudent.Id)
                return BadRequest(new { message = "Tylko lider zespołu może zapraszać studentów." });

            var maxMembers = group.MaxMembers ?? group.Project?.MaxNumberGroupMembers ?? 5;
            var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
            var pendingInviteCount = await _db.GroupRequests.CountAsync(gr =>
                gr.GroupId == groupId
                && gr.Type != null
                && (EF.Functions.ILike(gr.Type, "Invitation") || EF.Functions.ILike(gr.Type, "invite"))
                && gr.Status == GroupStatus.Pending);

            if (memberCount + pendingInviteCount >= maxMembers)
                return BadRequest(new { message = $"Zespół osiągnął limit miejsc ({maxMembers}). Zwiększ wielkość zespołu lub poczekaj na odpowiedź na wcześniejsze zaproszenia." });

            var normalizedEmail = Group.NormalizeStudentEmail(dto.Email);
            var invitedStudent = await _db.Students.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Email.ToLower() == normalizedEmail);
            if (invitedStudent == null)
                return NotFound(new { message = $"Nie znaleziono konta studenta o adresie {dto.Email.Trim()}. Student musi być zarejestrowany w systemie." });

            if (!Group.IsAllowedStudentEmail(invitedStudent.Email))
                return BadRequest(new { message = "Konto studenta w systemie ma niedozwoloną domenę e-mail. Użyj adresu @g.elearn.uz.zgora.pl lub @stud.uz.zgora.pl." });

            if (invitedStudent.GroupId == groupId)
                return BadRequest(new { message = "Ten student jest już członkiem tego zespołu." });

            if (Group.StudentBelongsToTeam(invitedStudent))
                return BadRequest(new { message = "Ten student należy już do innego zespołu." });

            if (invitedStudent.Id == leaderStudent.Id)
                return BadRequest(new { message = "Nie możesz zaprosić samego siebie." });

            var hasPendingInvitation = await _db.GroupRequests.AnyAsync(gr =>
                gr.GroupId == groupId
                && gr.StudentId == invitedStudent.Id
                && gr.Type != null
                && (EF.Functions.ILike(gr.Type, "Invitation") || EF.Functions.ILike(gr.Type, "invite"))
                && gr.Status == GroupStatus.Pending);

            if (hasPendingInvitation)
                return BadRequest(new { message = "Ten student ma już oczekujące zaproszenie do tego zespołu." });

            var creatorUser = await _db.Users.FindAsync(currentUserId);
            if (creatorUser == null) return Unauthorized();

            var invitation = new GroupRequest
            {
                GroupId = groupId,
                ProjectId = null,
                StudentId = invitedStudent.Id,
                CreatedByUserId = currentUserId,
                Type = GroupRequest.InvitationType,
                CreatedAt = DateTime.UtcNow,
                Status = GroupStatus.Pending,
                Group = group,
                Student = invitedStudent,
                CreatedByUser = creatorUser
            };

            _db.GroupRequests.Add(invitation);
            await _db.SaveChangesAsync();

            try
            {
                var leaderName = leaderStudent.User != null
                    ? $"{leaderStudent.User.Name} {leaderStudent.User.Surname}".Trim()
                    : creatorUser.Name + " " + creatorUser.Surname;
                var content = $"Zostałeś zaproszony do zespołu {group.Name} przez {leaderName}.";
                await _notifications.CreateNotificationAsync(invitedStudent.UserId, content, "/requests");
            }
            catch
            {
                // ignore notification errors
            }

            return Ok(new
            {
                message = "Zaproszenie zostało wysłane.",
                requestId = invitation.Id,
                groupId = invitation.GroupId,
                studentId = invitation.StudentId
            });
        }

        [HttpGet("my-invitations")]
        [Authorize(Policy = "StudentOnly")]
        /// <summary>
        /// Zwraca oczekujące zaproszenia do zespołów dla zalogowanego studenta.
        /// </summary>
        public async Task<ActionResult<List<GroupInvitationDto>>> GetMyInvitations()
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (student == null) return Forbid();

            if (Group.StudentBelongsToTeam(student))
                return Ok(new List<GroupInvitationDto>());

            var invitations = await _db.GroupRequests
                .Include(gr => gr.Group).ThenInclude(g => g!.Members)
                .Where(gr =>
                    gr.StudentId == student.Id
                    && gr.Type != null
                    && (EF.Functions.ILike(gr.Type, "Invitation") || EF.Functions.ILike(gr.Type, "invite"))
                    && gr.Status == GroupStatus.Pending)
                .Select(gr => new GroupInvitationDto
                {
                    RequestId = gr.Id,
                    GroupId = gr.GroupId,
                    GroupName = gr.Group != null ? gr.Group.Name : "",
                    MaxMembers = gr.Group != null ? gr.Group.MaxMembers : null,
                    MemberCount = gr.Group != null ? gr.Group.Members.Count : 0
                })
                .ToListAsync();

            return Ok(invitations);
        }

        [HttpPost("invitations/{requestId:int}/accept")]
        [Authorize(Policy = "StudentOnly")]
        /// <summary>
        /// Student akceptuje zaproszenie do zespołu.
        /// </summary>
        public async Task<IActionResult> AcceptInvitation(int requestId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (student == null) return Forbid();

            if (Group.StudentBelongsToTeam(student))
                return BadRequest("Należysz już do zespołu. Student może być tylko w jednym zespole na raz.");

            var request = await _db.GroupRequests
                .Include(gr => gr.Group).ThenInclude(g => g!.Members)
                .Include(gr => gr.Group).ThenInclude(g => g!.Project)
                .FirstOrDefaultAsync(gr => gr.Id == requestId);

            if (request == null) return NotFound();
            if (!GroupRequest.IsInvitationType(request.Type))
                return BadRequest("To nie jest zaproszenie do zespołu.");
            if (request.Status != GroupStatus.Pending)
                return BadRequest("Zaproszenie zostało już rozpatrzone.");
            if (!request.StudentId.HasValue || request.StudentId.Value != student.Id)
                return Forbid("Nie możesz zaakceptować cudzego zaproszenia.");

            var group = request.Group ?? await _db.Groups.Include(g => g.Members).Include(g => g.Project).FirstOrDefaultAsync(g => g.Id == request.GroupId);
            if (group == null) return NotFound("Zespół powiązany z zaproszeniem nie istnieje.");

            var maxMembers = group.MaxMembers ?? group.Project?.MaxNumberGroupMembers ?? 5;
            var memberCount = group.Members?.Count ?? await _db.Students.CountAsync(s => s.GroupId == group.Id);
            if (memberCount >= maxMembers)
                return BadRequest($"Zespół osiągnął limit członków ({maxMembers}).");

            student.GroupId = group.Id;
            request.Status = GroupStatus.Accepted;
            request.RespondedAt = DateTime.UtcNow;
            request.RespondedByUserId = currentUserId;

            _db.Students.Update(student);
            _db.GroupRequests.Update(request);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("invitations/{requestId:int}/decline")]
        [Authorize(Policy = "StudentOnly")]
        /// <summary>
        /// Student odrzuca zaproszenie do zespołu.
        /// </summary>
        public async Task<IActionResult> DeclineInvitation(int requestId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (student == null) return Forbid();

            var request = await _db.GroupRequests.FindAsync(requestId);
            if (request == null) return NotFound();
            if (!GroupRequest.IsInvitationType(request.Type))
                return BadRequest("To nie jest zaproszenie do zespołu.");
            if (request.Status != GroupStatus.Pending)
                return BadRequest("Zaproszenie zostało już rozpatrzone.");
            if (!request.StudentId.HasValue || request.StudentId.Value != student.Id)
                return Forbid("Nie możesz odrzucić cudzego zaproszenia.");

            request.Status = GroupStatus.Declined;
            request.RespondedAt = DateTime.UtcNow;
            request.RespondedByUserId = currentUserId;

            _db.GroupRequests.Update(request);
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
                    ProjectId = g.ProjectId,
                    MaxMembers = g.MaxMembers,
                    IsAccepted = g.IsAccepted.HasValue,
                    MemberCount = g.Members.Count,
                    Members = g.Members.Select(m => new MemberSummaryDto
                    {
                        Id = m.Id,
                        UserName = m.User.Name + " " + m.User.Surname,
                        Email = m.Email
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
                    ProjectId = g.ProjectId,
                    MaxMembers = g.MaxMembers,
                    IsAccepted = g.IsAccepted.HasValue,
                    MemberCount = g.Members.Count,
                    Members = g.Members.Select(m => new MemberSummaryDto
                    {
                        Id = m.Id,
                        UserName = m.User.Name + " " + m.User.Surname,
                        Email = m.Email
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (dto == null) return NotFound();
            return Ok(dto);
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        private int GetCurrentUserId()
        {
            if (!TryGetCurrentUserId(out var userId))
                throw new UnauthorizedAccessException("User not authenticated");
            return userId;
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
            if (group.LeaderId.HasValue && group.LeaderId.Value == userId) return true;

            // Group members can view/manage their group (for some actions)
            var isMember = await _db.Students.AnyAsync(s => s.GroupId == groupId && s.UserId == userId);
            return isMember;
        }
    }
}
