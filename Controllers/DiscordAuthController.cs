using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using AspNet.Security.OAuth.Discord;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.Services;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscordAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public DiscordAuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback))
            };
            
            return Challenge(properties, DiscordAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            if (!authenticateResult.Succeeded)
                return BadRequest("Authentication failed");

            var claims = authenticateResult.Principal.Claims;
            var discordId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            // Tìm hoặc tạo user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
            
            if (user == null)
            {
                // Tạo user mới từ Discord
                user = new User
                {
                    Username = username ?? $"discord_{discordId}",
                    Email = email ?? "",
                    DiscordId = discordId,
                    PassWords = Guid.NewGuid().ToString(),
                    Roles = "user",
                    Phone = "" // Discord không cung cấp phone
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Tạo JWT token
            var token = _jwtService.GenerateToken(user.Username, user.Roles);

            // Redirect về frontend với token
            return Redirect($"http://localhost:3000/auth/success?token={token}");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}