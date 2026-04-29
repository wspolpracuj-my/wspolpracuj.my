using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

namespace wspolpracujmy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Kontroler do pobierania typów spotkań dostępnych w aplikacji.
    /// </summary>
    public class MeetingTypesController : ControllerBase
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy kontroler typów spotkań z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public MeetingTypesController(AppDbContext db) => _db = db;

        [HttpGet]
        /// <summary>
        /// Zwraca listę wszystkich typów spotkań.
        /// </summary>
        /// <returns>Lista obiektów MeetingType.</returns>
        public async Task<IEnumerable<MeetingType>> Get() => await _db.Meeting_types.ToListAsync();

        [HttpGet("{id:int}")]
        /// <summary>
        /// Pobiera typ spotkania po identyfikatorze.
        /// </summary>
        /// <param name="id">Id typu spotkania.</param>
        /// <returns>Obiekt MeetingType lub NotFound.</returns>
        public async Task<ActionResult<MeetingType>> Get(int id)
        {
            var m = await _db.Meeting_types.FindAsync(id);
            if (m == null) return NotFound();
            return m;
        }
    }
}
