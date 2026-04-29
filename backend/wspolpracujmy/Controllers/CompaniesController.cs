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
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CompaniesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IEnumerable<Company>> Get() => await _db.Companies.ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Company>> Get(int id)
        {
            var c = await _db.Companies.FindAsync(id);
            if (c == null) return NotFound();
            return c;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<Company>> GetByUserId(int userId)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null) return NotFound();
            return company;
        }

        [HttpPost]
        public async Task<ActionResult<Company>> Post(Company company)
        {
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = company.Id }, company);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, Company company)
        {
            if (id != company.Id) return BadRequest();
            _db.Entry(company).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
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
