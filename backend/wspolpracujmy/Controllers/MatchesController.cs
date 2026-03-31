using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers;

[ApiController]
[Route("matches")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MatchesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMatchAsync([FromBody] CreateMatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyTin) || string.IsNullOrWhiteSpace(request.MatchedCompanyTin))
            return BadRequest(new { message = "companyTin and matchedCompanyTin are required." });

        var companyTin = request.CompanyTin.Trim();
        var matchedCompanyTin = request.MatchedCompanyTin.Trim();

        if (string.Equals(companyTin, matchedCompanyTin, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "companyTin and matchedCompanyTin must be different." });

        var companies = await _db.Companies
            .Where(c => c.Tin == companyTin || c.Tin == matchedCompanyTin)
            .Select(c => c.Tin)
            .ToListAsync();

        if (!companies.Contains(companyTin))
            return BadRequest(new { message = $"Company '{companyTin}' does not exist." });

        if (!companies.Contains(matchedCompanyTin))
            return BadRequest(new { message = $"Company '{matchedCompanyTin}' does not exist." });

        var alreadyExists = await _db.Matches.AnyAsync(m =>
            (m.CompanyTin == companyTin && m.MatchedCompanyTin == matchedCompanyTin)
            || (m.CompanyTin == matchedCompanyTin && m.MatchedCompanyTin == companyTin));

        if (alreadyExists)
            return Conflict(new { message = "Match between these companies already exists." });

        var match = new Match
        {
            CompanyTin = companyTin,
            MatchedCompanyTin = matchedCompanyTin,
            Status = StatusEnum.pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        var response = new MatchDto
        {
            CompanyTin = match.CompanyTin,
            MatchedCompanyTin = match.MatchedCompanyTin,
            Status = match.Status.ToString(),
            CreatedAt = match.CreatedAt
        };

        return Created($"/matches/{match.Id}", response);
    }

    public record CreateMatchRequest
    {
        public string CompanyTin { get; init; } = string.Empty;
        public string MatchedCompanyTin { get; init; } = string.Empty;
    }
}