using System.Collections.Generic;
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
    /// Kontroler do zarządzania firmami i ich danymi.
    /// </summary>
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtTokenService _jwtTokenService;
        /// <summary>
        /// Tworzy kontroler firm z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        /// <param name="jwtTokenService">Serwis generowania tokenów JWT.</param>
        public CompaniesController(AppDbContext db, JwtTokenService jwtTokenService)
        {
            _db = db;
            _jwtTokenService = jwtTokenService;
        }

        [HttpGet]
        /// <summary>
        /// Zwraca listę wszystkich firm z podstawowymi danymi.
        /// </summary>
        /// <returns>Listę DTO podsumowania firm.</returns>
        public async Task<IEnumerable<AdminCompanyDto>> Get() => await _db.Companies
            .Include(c => c.User)
            .OrderBy(c => c.CompanyName)
            .Select(c => new AdminCompanyDto
            {
                Id = c.Id,
                UserId = c.UserId,
                CompanyName = c.CompanyName,
                Login = c.User != null ? c.User.Login : string.Empty,
                ContactEmail = c.ContactEmail
            })
            .ToListAsync();

        [HttpGet("{id:int}")]
        /// <summary>
        /// Pobiera firmę po identyfikatorze z podstawowymi danymi.
        /// </summary>
        /// <param name="id">Id firmy.</param>
        /// <returns>DTO podsumowania firmy lub NotFound.</returns>
        public async Task<ActionResult<CompanySummaryDto>> Get(int id)
        {
            var c = await _db.Companies.FindAsync(id);
            if (c == null) return NotFound();
            return new CompanySummaryDto
            {
                Id = c.Id,
                UserId = c.UserId,
                CompanyName = c.CompanyName,
                ContactEmail = c.ContactEmail
            };
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<Company>> GetByUserId(int userId)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null) return NotFound();
            return company;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Tworzy nową firmę.
        /// </summary>
        /// <param name="dto">Dane firmy do utworzenia.</param>
        /// <returns>DTO podsumowania utworzonej firmy z kodem 201 Created.</returns>
        public async Task<ActionResult<CompanySummaryDto>> Post([FromBody] CreateCompanyDto dto)
        {
            // Only Admin or the user themself can create their company record
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var isAdmin = currentUser.Role == Role.Admin;
            if (!isAdmin && currentUserId != dto.UserId) return Forbid();

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"Użytkownik o id {dto.UserId} nie został znaleziony.");

            // ensure the user is a company account
            if (user.Role != Role.Company) return BadRequest("Użytkownik musi mieć rolę Company, aby utworzyć firmę.");

            // pre-check for existing company to avoid DB unique constraint exception
            var alreadyHas = await _db.Companies.AnyAsync(c => c.UserId == dto.UserId);
            if (alreadyHas) return Conflict($"Użytkownik o id {dto.UserId} już posiada firmę.");

            var company = new Company
            {
                UserId = dto.UserId,
                CompanyName = dto.CompanyName,
                ContactEmail = dto.ContactEmail,
                User = user
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();
            var summaryDto = new CompanySummaryDto
            {
                Id = company.Id,
                UserId = company.UserId,
                CompanyName = company.CompanyName,
                ContactEmail = company.ContactEmail
            };
            return CreatedAtAction(nameof(Get), new { id = company.Id }, summaryDto);
        }

        [HttpPut("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Aktualizuje istniejącą firmę.
        /// </summary>
        /// <param name="id">Id firmy do aktualizacji.</param>
        /// <param name="dto">Dane firmy do aktualizacji.</param>
        /// <returns>Brak treści (204) gdy zakończono pomyślnie.</returns>
        public async Task<IActionResult> Put(int id, [FromBody] CreateCompanyDto dto)
        {
            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound();
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var isAdmin = currentUser.Role == Role.Admin;
            // only admin or company owner can update
            if (!isAdmin && company.UserId != currentUserId) return Forbid();

            if (!await CanManageCompanyAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to update this company");

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"Użytkownik o id {dto.UserId} nie został znaleziony.");

            company.UserId = dto.UserId;
            company.CompanyName = dto.CompanyName;
            company.ContactEmail = dto.ContactEmail;
            company.User = user;

            _db.Entry(company).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("admin")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Tworzy nową firmę razem z kontem użytkownika. Endpoint przeznaczony dla administratora.
        /// </summary>
        /// <param name="dto">Dane firmy + dane logowania użytkownika.</param>
        /// <returns>Dane utworzonej firmy.</returns>
        public async Task<ActionResult<AdminCompanyDto>> PostAsAdmin([FromBody] AdminCompanyCreateDto dto)
        {
            if (!IsAdmin()) return Forbid();

            if (dto == null) return BadRequest("Brak danych firmy.");
            if (string.IsNullOrWhiteSpace(dto.CompanyName)) return BadRequest("Nazwa firmy jest wymagana.");
            if (string.IsNullOrWhiteSpace(dto.Login)) return BadRequest("Login jest wymagany.");
            if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Hasło jest wymagane.");

            var loginNormalized = dto.Login.Trim();
            if (await _db.Users.AnyAsync(u => u.Login == loginNormalized))
            {
                return Conflict(new { message = "Login jest już zajęty." });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                Name = dto.CompanyName.Trim(),
                Surname = "(firma)",
                Login = loginNormalized,
                PasswordHash = passwordHash,
                Role = Role.Company
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var company = new Company
            {
                UserId = user.Id,
                CompanyName = dto.CompanyName.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim(),
                User = user
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            var result = new AdminCompanyDto
            {
                Id = company.Id,
                UserId = company.UserId,
                CompanyName = company.CompanyName,
                Login = user.Login,
                ContactEmail = company.ContactEmail
            };

            return CreatedAtAction(nameof(Get), new { id = company.Id }, result);
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Usuwa firmę o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id firmy do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Companies.Include(co => co.User).FirstOrDefaultAsync(co => co.Id == id);
            if (c == null) return NotFound();

            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var isAdmin = currentUser.Role == Role.Admin;
            if (!isAdmin && c.UserId != currentUserId) return Forbid();

            // Usuń konto użytkownika — kaskada (FK) usunie również rekord firmy.
            if (c.User != null)
            {
                _db.Users.Remove(c.User);
            }
            else
            {
                _db.Companies.Remove(c);
            }
            await _db.SaveChangesAsync();
            return NoContent();
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

        private async Task<bool> CanManageCompanyAsync(int companyId, int userId)
        {
            var company = await _db.Companies.FindAsync(companyId);
            return company?.UserId == userId;
        }
    }
}
