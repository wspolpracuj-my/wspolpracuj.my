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
    /// Kontroler do pobierania tagów używanych przez projekty.
    /// </summary>
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _db;
        /// <summary>
        /// Tworzy kontroler tagów z kontekstem bazy danych.
        /// </summary>
        /// <param name="db">Kontekst bazy danych aplikacji.</param>
        public TagsController(AppDbContext db) => _db = db;

        [HttpGet]
        /// <summary>
        /// Zwraca listę wszystkich tagów.
        /// </summary>
        /// <returns>Lista obiektów Tag.</returns>
        public async Task<IEnumerable<Tag>> Get() => await _db.Tags.ToListAsync();

        [HttpGet("{id:int}")]
        /// <summary>
        /// Pobiera tag po identyfikatorze.
        /// </summary>
        /// <param name="id">Id tagu.</param>
        /// <returns>Obiekt Tag lub NotFound.</returns>
        public async Task<ActionResult<Tag>> Get(int id)
        {
            var t = await _db.Tags.FindAsync(id);
            if (t == null) return NotFound();
            return t;
        }
    }
}
