using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.DTO;
using System.Text.Json;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StyleSocialController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StyleSocialController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/StyleSocial/byUser/{userId}
        [HttpGet("byUser/{userId}")]
        public async Task<IActionResult> GetSocialByUser(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Style)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (user == null || user.Style == null)
                return NotFound(new { message = "Style not found for this user" });

            var socialList = string.IsNullOrEmpty(user.Style.Social)
                ? new List<SocialDto>()
                : JsonSerializer.Deserialize<List<SocialDto>>(user.Style.Social);

            return Ok(socialList);
        }

        // PUT /api/StyleSocial/byUser/{userId}
        [HttpPut("byUser/{userId}")]
        public async Task<IActionResult> UpdateSocialByUser(int userId, [FromBody] List<SocialDto> social)
        {
            var user = await _context.Users
                .Include(u => u.Style)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (user == null || user.Style == null)
                return NotFound(new { message = "Style not found for this user" });

            user.Style.Social = JsonSerializer.Serialize(social);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Updated successfully" });
        }
        
        [HttpPost("byUser/{userId}/upload-icon")]
        public async Task<IActionResult> UploadIcon(int userId, [FromForm] IFormFile iconFile)
        {
            if (iconFile == null || iconFile.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var user = await _context.Users.Include(u => u.Style)
                .FirstOrDefaultAsync(u => u.IdUser == userId);

            if (user == null || user.Style == null)
                return NotFound(new { message = "Style not found for this user" });

            // Lưu file vào wwwroot/uploads hoặc folder bất kỳ
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(iconFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await iconFile.CopyToAsync(stream);
            }

            // Trả về URL có thể dùng trên frontend
            var iconUrl = $"/uploads/{fileName}";

            return Ok(new { iconUrl });
        }

    }
}