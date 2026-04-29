using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    // NOT DONE YET - just a draft to show the general idea of handling group requests and related notifications. Will be refined later.
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Kontroler obsługujący żądania związane z dołączaniem/odpowiedziami dotyczącymi grup.
    /// </summary>
    public class GroupRequestsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly wspolpracujmy.Services.NotificationService _notifications;
        /// <summary>
        /// Tworzy kontroler żądań grupowych z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public GroupRequestsController(AppDbContext db, wspolpracujmy.Services.NotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpPost]
        /// <summary>
        /// Tworzy nowe żądanie związane z grupą (np. dołączenie lub zaproszenie).
        /// </summary>
        /// <param name="dto">Obiekt GroupRequest zawierający szczegóły żądania.</param>
        /// <returns>Utworzone żądanie z kodem 201 Created.</returns>
        public async Task<ActionResult<GroupRequest>> Post([FromBody] GroupRequest dto)
        {
            if (dto == null) return BadRequest();

            var group = await _db.Groups.Include(g => g.Project).FirstOrDefaultAsync(g => g.Id == dto.GroupId);
            if (group == null) return NotFound($"Group with id {dto.GroupId} not found.");

            var student = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == dto.StudentId);
            if (student == null) return NotFound($"Student with id {dto.StudentId} not found.");

            dto.CreatedAt = DateTime.UtcNow;
            _db.GroupRequests.Add(dto);

            // Determine notification(s) based on request type
            try
            {
                if (string.Equals(dto.Type, "join_request", StringComparison.OrdinalIgnoreCase))
                {
                    // If the request was created by the student themself -> notify group leader
                    var creatorUser = await _db.Users.FindAsync(dto.CreatedByUserId);

                    // find group leader (student->UserId) if exists
                    if (group.LeaderId.HasValue)
                    {
                        var leaderStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == group.LeaderId.Value);
                        if (leaderStudent != null)
                        {
                            var content = $"Student {student.User.Name} {student.User.Surname} zgłosił się do zespołu.";
                            var leaderUser = await _db.Users.FindAsync(leaderStudent.UserId);
                            if (leaderUser != null)
                            {
                                var notification = new Notification
                                {
                                    UserId = leaderStudent.UserId,
                                    Content = content,
                                    Status = NotificationStatus.NotRead,
                                    User = leaderUser,
                                    CreatedAt = System.DateTime.UtcNow,
                                    LinkTarget = $"group:{group.Id}"
                                };
                                _db.Notifications.Add(notification);
                            }
                        }
                    }

                    // additionally: if the request was created by the group's leader (team applying to project), notify project owner
                    if (dto.CreatedByUserId > 0 && group.LeaderId.HasValue)
                    {
                        var leaderStudent = await _db.Students.FirstOrDefaultAsync(s => s.Id == group.LeaderId.Value);
                        if (leaderStudent != null)
                        {
                            // leader's user id
                            var leaderUser = await _db.Users.FindAsync(leaderStudent.UserId);
                            if (leaderUser != null && leaderUser.Id == dto.CreatedByUserId)
                            {
                                // team applied to project -> notify project owner
                                var project = group.Project;
                                if (project != null)
                                {
                                    var company = await _db.Companies.FindAsync(project.CompanyId);
                                    if (company != null)
                                    {
                                        var content = $"Zespół {group.Name} zgłosił się do projektu {project.Topic}.";
                                        var companyUser = await _db.Users.FindAsync(company.UserId);
                                        if (companyUser != null)
                                        {
                                            var notification = new Notification
                                            {
                                                UserId = company.UserId,
                                                Content = content,
                                                Status = NotificationStatus.NotRead,
                                                User = companyUser,
                                                CreatedAt = System.DateTime.UtcNow,
                                                LinkTarget = $"project:{project.Id}"
                                            };
                                            _db.Notifications.Add(notification);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (string.Equals(dto.Type, "invite", StringComparison.OrdinalIgnoreCase))
                {
                    // invitation created by some user -> notify the invited student
                    var inviter = await _db.Users.FindAsync(dto.CreatedByUserId);
                    var invitedStudent = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == dto.StudentId);
                    if (inviter != null && invitedStudent != null)
                    {
                        var content = $"Student {inviter.Name} {inviter.Surname} zaprosił do zespołu {group.Name}.";
                        var invitedUser = invitedStudent.User;
                        if (invitedUser != null)
                        {
                            var notification = new Notification
                            {
                                UserId = invitedStudent.UserId,
                                Content = content,
                                Status = NotificationStatus.NotRead,
                                User = invitedUser,
                                CreatedAt = System.DateTime.UtcNow,
                                LinkTarget = $"group:{group.Id}"
                            };
                            _db.Notifications.Add(notification);
                        }
                    }
                }
            }
            catch
            {
                // swallow notification errors to not block request creation
            }

            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Post), new { id = dto.Id }, dto);
        }

        [HttpPost("{id:int}/respond")]
        public async Task<IActionResult> Respond(int id, [FromBody] Models.RespondGroupRequestDto dto)
        {
            if (dto == null) return BadRequest();

            var req = await _db.GroupRequests.FindAsync(id);
            if (req == null) return NotFound();

            if (req.Status == GroupStatus.Accepted || req.Status == GroupStatus.Declined)
            {
                // already handled
                return BadRequest("Request already responded");
            }

            var group = await _db.Groups.Include(g => g.Project).FirstOrDefaultAsync(g => g.Id == req.GroupId);
            var student = await _db.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == req.StudentId);

            var action = dto.Action?.Trim().ToLowerInvariant();
            if (action != "accept" && action != "decline") return BadRequest("Action must be 'accept' or 'decline'");

            req.Status = action == "accept" ? GroupStatus.Accepted : GroupStatus.Declined;
            req.RespondedAt = DateTime.UtcNow;

            // If accepted and this is a student responding to an invite/join_request, assign student to the group
            if (action == "accept" && req.Type != null && req.Type.Equals("invite", StringComparison.OrdinalIgnoreCase))
            {
                if (student != null)
                {
                    student.GroupId = req.GroupId;
                    _db.Students.Update(student);
                }
            }

            // notify relevant users
            try
            {
                // Handle join_request responses
                if (req.Type != null && req.Type.Equals("join_request", StringComparison.OrdinalIgnoreCase))
                {
                    // If responder is the company owner -> company deciding about group's admission to project
                    var project = group?.Project;
                    var company = project != null ? await _db.Companies.FindAsync(project.CompanyId) : null;
                    var responderIsCompany = company != null && dto.RespondedByUserId == company.UserId;

                    if (responderIsCompany)
                    {
                        // company accepted/declined the whole group application
                        if (group != null)
                        {
                            group.IsAccepted = action == "accept" ? GroupStatus.Accepted : GroupStatus.Declined;
                            _db.Groups.Update(group);
                        }

                        if (group != null)
                        {
                            // notify all members
                            var members = await _db.Students.Where(s => s.GroupId == group.Id).ToListAsync();
                            foreach (var m in members)
                            {
                                var content = action == "accept"
                                    ? $"Zespół {group.Name} został przyjęty do projektu {project?.Topic}."
                                    : $"Zespół {group.Name} nie został przyjęty do projektu {project?.Topic}.";
                                await _notifications.CreateNotificationAsync(m.UserId, content, $"project:{project?.Id}");
                            }
                        }
                    }
                    else
                    {
                        // normal flow: notify the student who requested
                        if (student != null)
                        {
                            var content = action == "accept"
                                ? $"Twoja prośba o dołączenie do zespołu {group?.Name} została zaakceptowana."
                                : $"Twoja prośba o dołączenie do zespołu {group?.Name} została odrzucona.";
                            await _notifications.CreateNotificationAsync(student.UserId, content, $"group:{group?.Id}");
                        }
                    }
                }
                else if (req.Type != null && req.Type.Equals("invite", StringComparison.OrdinalIgnoreCase))
                {
                    // notify the creator (inviter) that invite was accepted/declined
                    var creator = await _db.Users.FindAsync(req.CreatedByUserId);
                    var content = action == "accept"
                        ? $"Student {student?.User?.Name} {student?.User?.Surname} zaakceptował zaproszenie do zespołu {group?.Name}."
                        : $"Student {student?.User?.Name} {student?.User?.Surname} odrzucił zaproszenie do zespołu {group?.Name}.";
                    if (creator != null)
                        await _notifications.CreateNotificationAsync(creator.Id, content, $"group:{group?.Id}");
                }
            }
            catch
            {
                // ignore notification errors
            }

            _db.GroupRequests.Update(req);
            await _db.SaveChangesAsync();

            return Ok(req);
        }
    }
}
