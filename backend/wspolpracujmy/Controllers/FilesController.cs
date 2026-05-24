using System.Security.Claims;
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
    /// Kontroler do zarz?dzania plikami w Google Cloud Storage.
    /// </summary>
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly GcsService _gcsService;

        public FilesController(AppDbContext db, GcsService gcsService)
        {
            _db = db;
            _gcsService = gcsService;
        }

        [HttpPost("upload")]
        [Authorize]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        /// <summary>
        /// Uploaduje plik do Google Cloud Storage i zapisuje metadane w bazie.
        /// </summary>
        /// <param name="file">Plik do uploadowania.</param>
        /// <param name="teamId">ID zespo?u (opcjonalnie, wymagane dla admina i firm).</param>
        /// <param name="cancellationToken">Token anulowania operacji.</param>
        /// <returns>Utworzony ProjectFile z kodem 201 Created.</returns>
        /// <response code="201">Plik zosta? pomy?lnie uploadowany.</response>
        /// <response code="400">Plik jest wymagany lub brakuje teamId.</response>
        /// <response code="401">U?ytkownik nieuwierzytelniony.</response>
        /// <response code="403">U?ytkownik nie ma dost?pu do danego zespo?u.</response>
        public async Task<ActionResult<FileResponseDto>> Upload(IFormFile file, [FromQuery] int? teamId = null, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Nie wybrałeś pliku. Proszę wybrać plik do wysyłania." });
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            if (user == null)
            {
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });
            }

            int? resolvedTeamId = null;

            if (user.Role == Role.Admin)
            {
                if (teamId == null)
                {
                    return BadRequest(new { error = "Aby wysłać plik, musisz podać ID zespołu. Dodaj do adresu URL: ?teamId=123" });
                }
                resolvedTeamId = teamId;
            }
            else if (user.Role == Role.Company)
            {
                if (teamId == null)
                {
                    return BadRequest(new { error = "Aby wysłać plik, musisz podać ID zespołu. Dodaj do adresu URL: ?teamId=123" });
                }
                var hasAccess = await CompanyHasAccessToFileAsync(userId.Value, teamId.Value, cancellationToken);
                if (!hasAccess)
                {
                    return Forbid();
                }
                resolvedTeamId = teamId;
            }
            else
            {
                resolvedTeamId = await GetCurrentTeamIdAsync(userId.Value);
                if (resolvedTeamId == null)
                {
                    return BadRequest(new { error = "Nie jesteś członkiem żadnego zespołu. Skontaktuj się z administratorem." });
                }
            }

            await using var stream = file.OpenReadStream();
            var objectName = await _gcsService.UploadFileAsync(
                stream,
                file.FileName,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                resolvedTeamId.Value,
                cancellationToken);

            var projectFile = new ProjectFile
            {
                OriginalFileName = Path.GetFileName(file.FileName),
                GcsObjectName = objectName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                TeamId = resolvedTeamId.Value,
                UploadDate = DateTime.UtcNow
            };

            _db.ProjectFiles.Add(projectFile);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = MapToDto(projectFile);
            return CreatedAtAction(nameof(GetMetadata), new { fileId = projectFile.Id }, dto);
        }

        [HttpGet("download/{fileId:guid}")]
        [Authorize]
        [Produces("application/json")]
        /// <summary>
        /// Generuje Signed URL do pobrania pliku z Google Cloud Storage.
        /// </summary>
        /// <param name="fileId">ID pliku do pobrania.</param>
        /// <param name="cancellationToken">Token anulowania operacji.</param>
        /// <returns>Obiekt zawieraj?cy signed URL wa?ny przez 15 minut.</returns>
        /// <response code="200">URL zosta? pomy?lnie wygenerowany.</response>
        /// <response code="401">U?ytkownik nieuwierzytelniony.</response>
        /// <response code="403">U?ytkownik nie ma dost?pu do tego pliku.</response>
        /// <response code="404">Plik nie zosta? znaleziony.</response>
        public async Task<ActionResult<object>> Download(Guid fileId, CancellationToken cancellationToken)
        {
            var projectFile = await _db.ProjectFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (projectFile == null)
            {
                return NotFound(new { error = "Plik nie został znaleziony." });
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            if (user == null)
            {
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });
            }

            var hasAccess = false;

            if (user.Role == Role.Admin)
            {
                hasAccess = true;
            }
            else if (user.Role == Role.Student)
            {
                var teamId = await GetCurrentTeamIdAsync(userId.Value);
                hasAccess = teamId.HasValue && teamId.Value == projectFile.TeamId;
            }
            else if (user.Role == Role.Company)
            {
                hasAccess = await CompanyHasAccessToFileAsync(userId.Value, projectFile.TeamId, cancellationToken);
            }

            if (!hasAccess)
            {
                return StatusCode(403, new { error = "Nie masz dost?pu do tego pliku." });
            }

            var downloadUrl = await _gcsService.GenerateDownloadUrlAsync(projectFile.GcsObjectName);
            return Ok(new { url = downloadUrl });
        }

        [HttpDelete("{fileId:guid}")]
        [Authorize]
        /// <summary>
        /// Usuwa plik z Google Cloud Storage i bazy danych.
        /// </summary>
        /// <param name="fileId">ID pliku do usuni?cia.</param>
        /// <param name="cancellationToken">Token anulowania operacji.</param>
        /// <returns>Brak tre?ci (204) gdy usuni?to pomy?lnie.</returns>
        /// <response code="204">Plik zosta? pomy?lnie usuni?ty.</response>
        /// <response code="401">U?ytkownik nieuwierzytelniony.</response>
        /// <response code="403">U?ytkownik nie ma dost?pu do usuni?cia tego pliku.</response>
        /// <response code="404">Plik nie zosta? znaleziony.</response>
        public async Task<IActionResult> Delete(Guid fileId, CancellationToken cancellationToken)
        {
            var projectFile = await _db.ProjectFiles
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (projectFile == null)
            {
                return NotFound(new { error = "Plik nie został znaleziony." });
            }

            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            if (user == null)
            {
                return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });
            }

            var hasAccess = false;

            if (user.Role == Role.Admin)
            {
                hasAccess = true;
            }
            else if (user.Role == Role.Student)
            {
                var teamId = await GetCurrentTeamIdAsync(userId.Value);
                hasAccess = teamId.HasValue && teamId.Value == projectFile.TeamId;
            }
            else if (user.Role == Role.Company)
            {
                hasAccess = await CompanyHasAccessToFileAsync(userId.Value, projectFile.TeamId, cancellationToken);
            }

            if (!hasAccess)
            {
                return StatusCode(403, new { error = "Nie masz dostępu do usunięcia tego pliku." });
            }

            _db.ProjectFiles.Remove(projectFile);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                await _gcsService.DeleteFileAsync(projectFile.GcsObjectName, cancellationToken);
            }
            catch
            {
            }

            return NoContent();
        }

        [HttpGet]
        [Authorize]
        [Produces("application/json")]
        /// <summary>
        /// Zwraca list? plik?w dla danego zespo?u (lub dla zalogowanego studenta).
        /// </summary>
        /// <param name="teamId">Id zespo?u. Dla admina/firmy wymagane; dla studenta pomijane.</param>
        public async Task<ActionResult<IEnumerable<FileResponseDto>>> Get([FromQuery] int? teamId = null)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null) return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            int? resolvedTeamId = null;

            if (user.Role == Role.Admin)
            {
                if (teamId == null) return BadRequest(new { error = "Dla administratora podaj parametr ?teamId=..." });
                resolvedTeamId = teamId;
            }
            else if (user.Role == Role.Company)
            {
                if (teamId == null) return BadRequest(new { error = "Dla firmy podaj parametr ?teamId=..." });
                var hasAccess = await CompanyHasAccessToFileAsync(userId.Value, teamId.Value, CancellationToken.None);
                if (!hasAccess) return Forbid();
                resolvedTeamId = teamId;
            }
            else // Student
            {
                resolvedTeamId = await GetCurrentTeamIdAsync(userId.Value);
                if (resolvedTeamId == null) return BadRequest(new { error = "Nie jesteś członkiem żadnego zespołu." });
            }

            var files = await _db.ProjectFiles
                .AsNoTracking()
                .Where(f => f.TeamId == resolvedTeamId.Value)
                .OrderByDescending(f => f.UploadDate)
                .Select(f => new FileResponseDto
                {
                    Id = f.Id,
                    FileName = f.OriginalFileName,
                    ContentType = f.ContentType,
                    TeamId = f.TeamId,
                    UploadDate = f.UploadDate
                })
                .ToListAsync();

            return Ok(files);
        }

        [HttpGet("{fileId:guid}")]
        [Authorize]
        [Produces("application/json")]
        /// <summary>
        /// Zwraca metadane pliku (bez linku do pobrania).
        /// </summary>
        public async Task<ActionResult<FileResponseDto>> GetMetadata(Guid fileId)
        {
            var projectFile = await _db.ProjectFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fileId);
            if (projectFile == null) return NotFound(new { error = "Plik nie został znaleziony." });

            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null) return Unauthorized(new { error = "Sesja wygasła. Zaloguj się ponownie." });

            var hasAccess = false;
            if (user.Role == Role.Admin) hasAccess = true;
            else if (user.Role == Role.Student)
            {
                var teamId = await GetCurrentTeamIdAsync(userId.Value);
                hasAccess = teamId.HasValue && teamId.Value == projectFile.TeamId;
            }
            else if (user.Role == Role.Company)
            {
                hasAccess = await CompanyHasAccessToFileAsync(userId.Value, projectFile.TeamId, CancellationToken.None);
            }

            if (!hasAccess) return StatusCode(403, new { error = "Nie masz dostępu do tego pliku." });

            return Ok(MapToDto(projectFile));
        }

        private static FileResponseDto MapToDto(ProjectFile file)
        {
            return new FileResponseDto
            {
                Id = file.Id,
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                TeamId = file.TeamId,
                UploadDate = file.UploadDate
            };
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            /// Pobiera ID zalogowanego u?ytkownika z JWT claims.
            var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdStr, out var userId))
            {
                return null;
            }

            return await Task.FromResult(userId);
        }

        private async Task<int?> GetCurrentTeamIdAsync(int currentUserId)
        {
            /// Pobiera ID zespo?u (grupy) zalogowanego u?ytkownika lub z JWT claim "teamId".
            var teamIdStr = User?.FindFirst("teamId")?.Value
                         ?? User?.FindFirst("groupId")?.Value;

            if (int.TryParse(teamIdStr, out var parsedTeamId))
            {
                return await Task.FromResult(parsedTeamId);
            }

            return await _db.Students
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .Select(s => s.GroupId)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> CompanyHasAccessToFileAsync(int companyUserId, int teamId, CancellationToken cancellationToken)
        {
            /// Sprawdza, czy firma ma dost?p do pliku na podstawie przypisanych projekt?w do grupy.
            var hasAccess = await _db.Projects
                .AsNoTracking()
                .Include(p => p.Groups)
                .Where(p => p.Company != null && p.Company.UserId == companyUserId)
                .SelectMany(p => p.Groups)
                .AnyAsync(g => g.Id == teamId, cancellationToken);

            return hasAccess;
        }
    }
}