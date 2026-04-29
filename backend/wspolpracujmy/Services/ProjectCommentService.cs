using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za pobieranie komentarzy i powiązanych odpowiedzi dla projektów.
    /// </summary>
    public class ProjectCommentService
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy serwis komentarzy z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public ProjectCommentService(AppDbContext db) => _db = db;

        /// <summary>
        /// Pobiera komentarze wraz z ich odpowiedziami dla zadanego projektu.
        /// </summary>
        /// <param name="projectId">Identyfikator projektu.</param>
        /// <returns>Lista DTO komentarzy z odpowiedziami.</returns>
        public async Task<List<CommentWithResponsesDto>> GetCommentsForProjectAsync(int projectId)
        {
            return await _db.Comments
                .Where(c => c.ProjectId == projectId)
                .Include(c => c.User)
                .Include(c => c.Responses).ThenInclude(r => r.User)
                .Select(c => new CommentWithResponsesDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User.Name + " " + c.User.Surname,
                    GroupId = _db.Students.Where(s => s.UserId == c.UserId).Select(s => (int?)s.GroupId).FirstOrDefault(),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    Responses = c.Responses.Select(r => new ResponseDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User.Name + " " + r.User.Surname,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                })
                .ToListAsync();
        }

        /// <summary>
        /// Pobiera komentarze dla projektu, filtrowane według grupy.
        /// </summary>
        /// <param name="projectId">Id projektu.</param>
        /// <param name="groupId">Id grupy.</param>
        /// <returns>Lista DTO komentarzy z odpowiedziami należących do grupy.</returns>
        public async Task<List<CommentWithResponsesDto>> GetCommentsForProjectByGroupAsync(int projectId, int groupId)
        {
            return await _db.Comments
                .Where(c => c.ProjectId == projectId)
                .Where(c => _db.Students.Any(s => s.UserId == c.UserId && s.GroupId == groupId))
                .Include(c => c.User)
                .Include(c => c.Responses).ThenInclude(r => r.User)
                .Select(c => new CommentWithResponsesDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User.Name + " " + c.User.Surname,
                    GroupId = _db.Students.Where(s => s.UserId == c.UserId).Select(s => (int?)s.GroupId).FirstOrDefault(),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    Responses = c.Responses.Select(r => new ResponseDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User.Name + " " + r.User.Surname,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}
