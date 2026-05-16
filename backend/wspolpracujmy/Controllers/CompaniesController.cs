using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.DTOs;
using wspolpracujmy.Models;

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
        /// <summary>
        /// Tworzy kontroler firm z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public CompaniesController(AppDbContext db) => _db = db;

        [HttpGet]
        /// <summary>
        /// Zwraca listę wszystkich firm z podstawowymi danymi.
        /// </summary>
        /// <returns>Listę DTO podsumowania firm.</returns>
        public async Task<IEnumerable<CompanySummaryDto>> Get() => await _db.Companies
            .Select(c => new CompanySummaryDto
            {
                Id = c.Id,
                UserId = c.UserId,
                CompanyName = c.CompanyName,
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

            int currentUserId = GetCurrentUserId();
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

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        /// <summary>
        /// Usuwa firmę o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id firmy do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Companies.FindAsync(id);
            if (c == null) return NotFound();

<<<<<<< HEAD
            int currentUserId = GetCurrentUserId();
            if (!await CanManageCompanyAsync(id, currentUserId) && !IsAdmin())
                return Forbid("No permission to delete this company");
=======
            var userIdStr = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("id")?.Value
                         ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();
            var currentUser = await _db.Users.FindAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var isAdmin = currentUser.Role == Role.Admin;
            if (!isAdmin && c.UserId != currentUserId) return Forbid();
>>>>>>> origin/StudentsApi+AuthByRole

            _db.Companies.Remove(c);
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
