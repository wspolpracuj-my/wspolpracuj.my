using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers;

[ApiController]
[Route("services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetServicesAsync()
    {
        var services = await _db.Services
            .AsQueryable()
            .OrderBy(s => s.Name)
            .Select(s => new { id = s.Id, name = s.Name })
            .ToListAsync();

        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetServiceByIdAsync([FromRoute] int id)
    {
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);
        if (service == null)
            return NotFound();

        return Ok(new { id = service.Id, name = service.Name });
    }
}
