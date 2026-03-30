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
    public async Task<IActionResult> GetAll([FromQuery] int? service_id, [FromQuery] int? offer_id, [FromQuery] string? location)
    {
        IQueryable<Company> query = _db.Companies
            .AsQueryable()
            .Include(c => c.Service)
            .Include(c => c.Offer)
            .Include(c => c.Users)
            .Include(c => c.MatchesInitiated)
            .Include(c => c.MatchesReceived);

        if (service_id.HasValue)
            query = query.Where(c => c.ServiceId == service_id.Value);

        if (offer_id.HasValue)
            query = query.Where(c => c.OfferId == offer_id.Value);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(c => c.Location != null && EF.Functions.ILike(c.Location, $"%{location}%"));

        var companies = await query
            .Select(c => new CompanyDto
            {
                Tin = c.Tin,
                Name = c.Name,
                Description = c.Description,
                Website = c.Website,
                ContactEmail = c.ContactEmail,
                Location = c.Location,
                CreatedAt = c.CreatedAt,
                Service = c.Service == null ? null : new ServiceDto { Id = c.Service.Id, Name = c.Service.Name },
                Offer = c.Offer == null ? null : new ServiceDto { Id = c.Offer.Id, Name = c.Offer.Name },
                Users = c.Users.Select(u => new UserDto { Id = u.Id, Mail = u.Mail, Verified = u.Verified }).ToList(),
                MatchesInitiated = c.MatchesInitiated.Select(m => new MatchDto { Id = m.Id, CompanyTin = m.CompanyTin, MatchedCompanyTin = m.MatchedCompanyTin, Status = m.Status.ToString(), CreatedAt = m.CreatedAt }).ToList(),
                MatchesReceived = c.MatchesReceived.Select(m => new MatchDto { Id = m.Id, CompanyTin = m.CompanyTin, MatchedCompanyTin = m.MatchedCompanyTin, Status = m.Status.ToString(), CreatedAt = m.CreatedAt }).ToList()
            })
            .ToListAsync();

        return Ok(companies);
    }
}
