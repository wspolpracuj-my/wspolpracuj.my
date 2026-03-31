using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CompaniesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompaniesAsync([FromQuery] int? service_id, [FromQuery] int? offer_id, [FromQuery] string? location, [FromQuery] string? q)
    {
        // Build a base query with related data needed by the response DTO.
        IQueryable<Company> query = _db.Companies
            .AsQueryable()
            .Include(c => c.Service)
            .Include(c => c.Offer)
            .Include(c => c.Users);

        // Apply optional filters only when query params are provided.
        if (service_id.HasValue)
            query = query.Where(c => c.ServiceId == service_id.Value);

        if (offer_id.HasValue)
            query = query.Where(c => c.OfferId == offer_id.Value);

        // Case-insensitive partial match for location.
        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(c => c.Location != null && EF.Functions.ILike(c.Location, $"%{location}%"));

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{q}%"));

        // Split query avoids cartesian explosion when loading multiple collections.
        var companiesEntities = await query
            .AsSplitQuery() 
            .ToListAsync();

        // Map entity graph to API DTOs.
        var companies = companiesEntities.Select(c => new CompanyDto
        {
            Tin = c.Tin,
            Name = c.Name,
            Description = c.Description,
            Website = c.Website,
            ContactEmail = c.ContactEmail,
            Location = c.Location,
            CreatedAt = c.CreatedAt,
            // Service = c.Service == null ? null : new ServiceDto { Id = c.Service.Id, Name = c.Service.Name },
            // Offer = c.Offer == null ? null : new ServiceDto { Id = c.Offer.Id, Name = c.Offer.Name },
            Service = c.Service == null ? null : new ServiceDto { Name = c.Service.Name },
            Offer = c.Offer == null ? null : new ServiceDto { Name = c.Offer.Name },
        })
        .ToList();

        return Ok(companies);
    }
    [HttpGet("{tin}")]
    public async Task<IActionResult> GetCompanyByTin([FromRoute] string tin)
    {
        var company = await _db.Companies
            .AsQueryable()
            .Include(c => c.Service)
            .Include(c => c.Offer)
            .Include(c => c.Users)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Tin == tin);

        if (company == null)
            return NotFound();


        // Map entity graph to API DTOs.
        var companies = new CompanyDto
        {
            Tin = company.Tin,
            Name = company.Name,
            Description = company.Description,
            Website = company.Website,
            ContactEmail = company.ContactEmail,
            Location = company.Location,
            CreatedAt = company.CreatedAt,
            Service = company.Service == null ? null : new ServiceDto { Name = company.Service.Name },
            Offer = company.Offer == null ? null : new ServiceDto { Name = company.Offer.Name },
        };
    
    return Ok(companies);
    }
}
