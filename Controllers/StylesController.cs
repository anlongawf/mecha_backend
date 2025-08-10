using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StylesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StylesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/styles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StyleModel>>> GetStyles()
        {
            return await _context.Styles.ToListAsync();
        }

        // GET: api/styles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<StyleModel>> GetStyle(string id)
        {
            var style = await _context.Styles.FindAsync(id);
            if (style == null) return NotFound();
            return style;
        }

        // POST: api/styles
        [HttpPost]
        public async Task<ActionResult<StyleModel>> CreateStyle(StyleModel style)
        {
            _context.Styles.Add(style);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetStyle), new { id = style.StyleId }, style);
        }

        // PUT: api/styles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStyle(string id, StyleModel style)
        {
            if (id != style.StyleId) return BadRequest();

            _context.Entry(style).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Styles.Any(e => e.StyleId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/styles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStyle(string id)
        {
            var style = await _context.Styles.FindAsync(id);
            if (style == null) return NotFound();

            _context.Styles.Remove(style);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
