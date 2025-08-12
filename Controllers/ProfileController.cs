using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;

[ApiController]
[Route("/{username}")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetProfile(string username)
    {
        var user = _context.Users
            .Include(u => u.Style) // Lấy luôn style
            .FirstOrDefault(u => u.Username == username);

        if (user == null)
            return NotFound(new { message = "User not found" });

        if (user.Style == null)
            return Ok(new { username = user.Username, email = user.Email, style = (object?)null });

        return Ok(new
        {
            user.Style.StyleId,
            user.Style.ProfileAvatar,
            user.Style.Background,
            user.Style.Audio,
            user.Style.CustomCursor,
            user.Style.Description,
            user.Style.Username,
            user.Style.EffectUsername,
            user.Style.Location
        });
    }
}