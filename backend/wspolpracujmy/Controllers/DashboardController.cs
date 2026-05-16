using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;

namespace wspolpracujmy.Controllers
{
    /// <summary>
    /// Kontroler administracyjny dostarczający endpointy raportowe dla dashboardu.
    /// Dostęp do wszystkich akcji tego kontrolera jest ograniczony do użytkowników z rolą `Admin`.
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public DashboardController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Zwraca projekty, w których aktualna liczba przypisanych grup równa się wartości `MaxGroups`.
        /// Response: obiekt zawierający `total_count` oraz listę `projects` z polami: ProjectName, CompanyName, CurrentGroupsCount, MaxGroups.
        /// Implementacja: zapytanie LINQ agregujące liczbę grup per projekt i porównujące z polem `MaxGroups`.
        /// </summary>
        [HttpGet("full-projects")]
        public async Task<ActionResult<FullProjectsResponseDto>> GetFullProjects()
        {
            // Projekty, które mają ustawiony MaxGroups i liczba aktualnych grup == MaxGroups
            var projectsQuery = _dbContext.Projects
                .Where(p => p.MaxGroups.HasValue)
                .Select(p => new
                {
                    Project = p,
                    CurrentGroups = _dbContext.Groups.Count(g => g.ProjectId == p.Id)
                })
                .Where(x => x.CurrentGroups == x.Project.MaxGroups);

            var projects = await projectsQuery
                .Select(x => new FullProjectDto
                {
                    ProjectName = x.Project.Topic,
                    CompanyName = x.Project.Company.CompanyName,
                    CurrentGroupsCount = x.CurrentGroups,
                    MaxGroups = x.Project.MaxGroups ?? 0
                })
                .ToListAsync();

            var response = new FullProjectsResponseDto
            {
                TotalCount = projects.Count,
                Projects = projects
            };

            return Ok(response);
        }

        /// <summary>
        /// Zwraca TOP 5 nazw firm, które posiadają najwięcej projektów.
        /// Response: prosta tablica stringów z nazwami firm.
        /// Implementacja: grupowanie projektów po `CompanyName` i wybór 5 największych.
        /// </summary>
        [HttpGet("top-companies-by-projects")]
        public async Task<ActionResult<string[]>> GetTopCompaniesByProjects()
        {
            var top = await _dbContext.Projects
                .Include(p => p.Company)
                .GroupBy(p => p.Company.CompanyName)
                .Select(g => new { CompanyName = g.Key, ProjectsCount = g.Count() })
                .OrderByDescending(x => x.ProjectsCount)
                .ThenBy(x => x.CompanyName)
                .Take(5)
                .Select(x => x.CompanyName)
                .ToArrayAsync();

            return Ok(top);
        }

        /// <summary>
        /// Zwraca TOP 5 nazw firm, które mają największą łączną liczbę grup we wszystkich swoich projektach.
        /// Response: prosta tablica stringów z nazwami firm.
        /// Implementacja: zsumuj liczbę grup per projekt, a następnie agreguj sumy per firma.
        /// </summary>
        [HttpGet("top-companies-by-groups")]
        public async Task<ActionResult<string[]>> GetTopCompaniesByGroups()
        {
            var perProjectGroupCounts = _dbContext.Projects
                .GroupJoin(_dbContext.Groups,
                    p => p.Id,
                    g => g.ProjectId,
                    (p, groups) => new { CompanyName = p.Company.CompanyName, GroupCount = groups.Count() });

            var top = await perProjectGroupCounts
                .GroupBy(x => x.CompanyName)
                .Select(g => new { CompanyName = g.Key, TotalGroups = g.Sum(x => x.GroupCount) })
                .OrderByDescending(x => x.TotalGroups)
                .ThenBy(x => x.CompanyName)
                .Take(5)
                .Select(x => x.CompanyName)
                .ToArrayAsync();

            return Ok(top);
        }
    }
}
