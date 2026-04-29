using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Services
{
    /// <summary>
    /// Serwis do pobierania podsumowań projektów i agregacji powiązanych danych.
    /// </summary>
    public class ProjectService
    {
        private readonly AppDbContext _db;

        /// <summary>
        /// Tworzy serwis projektów z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public ProjectService(AppDbContext db)
        {
            _db = db;
        }

        // pobranie projektu firmy o id=companyId: id, temat, liczba grup, max grup
        /// <summary>
        /// Pobiera podsumowania projektów przypisanych do danej firmy.
        /// </summary>
        /// <param name="companyId">Id firmy.</param>
        /// <returns>Lista DTO z podsumowaniami projektów.</returns>
        public async Task<List<ProjectSummaryDto>> GetProjectsForCompanyAsync(int companyId)
        {
            var query = _db.Projects
                .Where(p => p.CompanyId == companyId)
                .GroupJoin(
                    _db.Groups,
                    p => p.Id,
                    g => g.ProjectId,
                    (p, gs) => new ProjectSummaryDto
                    {
                        Id = p.Id,
                        Topic = p.Topic,
                        CurrentGroupsCount = gs.Count(),
                        MaxGroups = p.MaxGroups ?? 0
                    }
                );

            return await query.ToListAsync();
        }

        // lista wszystkich projektów: id, temat, liczba grup, max grup
        /// <summary>
        /// Pobiera podsumowania wszystkich projektów w systemie.
        /// </summary>
        /// <returns>Lista DTO z podsumowaniami projektów.</returns>
        public async Task<List<ProjectSummaryDto>> GetAllProjectSummariesAsync()
        {
            var query = _db.Projects
                .GroupJoin(
                    _db.Groups,
                    p => p.Id,
                    g => g.ProjectId,
                    (p, gs) => new ProjectSummaryDto
                    {
                        Id = p.Id,
                        Topic = p.Topic,
                        CurrentGroupsCount = gs.Count(),
                        MaxGroups = p.MaxGroups ?? 0
                    }
                );

            return await query.ToListAsync();
        }
    }
}
