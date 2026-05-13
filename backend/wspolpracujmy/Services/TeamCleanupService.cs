using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Services
{
    /// <summary>
    /// Service for cleaning up team/group resources including GCS files.
    /// </summary>
    public class TeamCleanupService
    {
        private readonly AppDbContext _context;
        private readonly GcsService _gcsService;

        public TeamCleanupService(AppDbContext context, GcsService gcsService)
        {
            _context = context;
            _gcsService = gcsService;
        }

        /// <summary>
        /// Deletes a team (group) and all associated files from both database and GCS.
        /// </summary>
        /// <param name="teamId">The ID of the team/group to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteTeamAndCleanupFilesAsync(int teamId, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var projectFiles = await _context.ProjectFiles
                    .AsNoTracking()
                    .Where(pf => pf.TeamId == teamId)
                    .Select(pf => pf.GcsObjectName)
                    .ToListAsync(cancellationToken);

                if (projectFiles.Count > 0)
                {
                    await _gcsService.DeleteFilesAsync(projectFiles, cancellationToken);
                }

                var group = await _context.Groups.FindAsync(new object[] { teamId }, cancellationToken: cancellationToken);
                if (group != null)
                {
                    _context.Groups.Remove(group);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
