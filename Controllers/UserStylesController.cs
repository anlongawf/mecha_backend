using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.DTO;
using System.Text.Json;
[ApiController]
[Route("api/[controller]")]
public class UserStylesController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserStylesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/UserStyles/19
    [HttpGet("{idUser}")]
    public async Task<IActionResult> GetUserStyle(int idUser)
    {
        var style = await _context.UserStyles.FirstOrDefaultAsync(x => x.IdUser == idUser);
        if (style == null)
            return NotFound();

        var dto = new UserStyleDto
        {
            IdUser = style.IdUser,
            Styles = JsonSerializer.Deserialize<Dictionary<string, object>>(style.Styles)
        };

        return Ok(dto);
    }

    // POST: api/UserStyles
    [HttpPost]
    public async Task<IActionResult> CreateUserStyle([FromBody] UserStyleDto dto)
    {
        if (dto == null) return BadRequest("Invalid body");

        var entity = new UserStyle
        {
            IdUser = dto.IdUser,
            Styles = JsonSerializer.Serialize(dto.Styles)
        };

        _context.UserStyles.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { entity.StyleId, entity.IdUser, dto.Styles });
    }

    // PUT: api/UserStyles/19
    [HttpPut("{idUser}")]
    public async Task<IActionResult> UpdateUserStyle(int idUser, [FromBody] UserStyleDto dto)
    {
        var style = await _context.UserStyles.FirstOrDefaultAsync(x => x.IdUser == idUser);
        if (style == null) return NotFound();

        style.Styles = JsonSerializer.Serialize(dto.Styles);
        await _context.SaveChangesAsync();

        return Ok(new { style.IdUser, dto.Styles });
    }

    // DELETE: api/UserStyles/19
    [HttpDelete("{idUser}")]
    public async Task<IActionResult> DeleteUserStyle(int idUser)
    {
        var style = await _context.UserStyles.FirstOrDefaultAsync(x => x.IdUser == idUser);
        if (style == null) return NotFound();

        _context.UserStyles.Remove(style);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Deleted successfully" });
    }
}
