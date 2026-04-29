using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        /// <summary>
        /// Tworzy nową firmę.
        /// </summary>
        /// <param name="dto">Dane firmy do utworzenia.</param>
        /// <returns>DTO podsumowania utworzonej firmy z kodem 201 Created.</returns>
        public async Task<ActionResult<CompanySummaryDto>> Post([FromBody] CreateCompanyDto dto)
        {
            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"User with id {dto.UserId} not found.");

            // ensure the user is a company account
            if (user.Role != Role.Company) return BadRequest("User must have Role.Company to create a company.");

            // pre-check for existing company to avoid DB unique constraint exception
            var alreadyHas = await _db.Companies.AnyAsync(c => c.UserId == dto.UserId);
            if (alreadyHas) return Conflict($"User with id {dto.UserId} already has a company.");

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

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound($"User with id {dto.UserId} not found.");

            company.UserId = dto.UserId;
            company.CompanyName = dto.CompanyName;
            company.ContactEmail = dto.ContactEmail;
            company.User = user;

            _db.Entry(company).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        /// <summary>
        /// Usuwa firmę o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Id firmy do usunięcia.</param>
        /// <returns>Brak treści (204) lub NotFound.</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Companies.FindAsync(id);
            if (c == null) return NotFound();
            _db.Companies.Remove(c);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
