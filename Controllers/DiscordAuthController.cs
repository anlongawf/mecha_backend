using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using AspNet.Security.OAuth.Discord;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.Services;
using System.Text.Json;

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
                RedirectUri = Url.Action(nameof(CallbackPopup))
            };
            return Challenge(properties, DiscordAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> CallbackPopup()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return Content("<script>window.opener.postMessage({ error: 'Auth failed' }, '*'); window.close();</script>", "text/html");

            var claims = authenticateResult.Principal.Claims;
            var discordId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);

            if (user == null)
            {
                // Tạo StyleModel mặc định trước
                var defaultStyleId = Guid.NewGuid().ToString();
                var defaultStyleModel = new StyleModel
                {
                    StyleId = defaultStyleId,
                    ProfileAvatar = null,
                    Background = null,
                    Audio = null,
                    CustomCursor = null,
                    Description = "Welcome to Mecha!",
                    Username = username ?? $"discord_{discordId}",
                    Location = null,
                    AudioImage = null,
                    AudioTitle = null,
                    Social = null
                };

                _context.Styles.Add(defaultStyleModel);

                // Tạo user mới với StyleId
                user = new User
                {
                    Username = username ?? $"discord_{discordId}",
                    Email = email ?? "",
                    DiscordId = discordId,
                    password = Guid.NewGuid().ToString(),
                    Roles = "user",
                    Phone = "",
                    StyleId = defaultStyleId,
                    Premium = false,
                    IsVerified = false
                };
                _context.Users.Add(user);
                
                // Lưu để có IdUser
                await _context.SaveChangesAsync();

                // Tạo JSON object mặc định cho UserStyle
                var defaultStylesJson = JsonSerializer.Serialize(new
                {
                    theme = "default",
                    color_scheme = "light",
                    layout = "standard",
                    custom_css = "",
                    preferences = new
                    {
                        show_avatar = true,
                        show_background = true,
                        enable_animations = true
                    }
                });

                // Tạo UserStyle với JSON data
                var userStyle = new UserStyle
                {
                    IdUser = user.IdUser,
                    Styles = defaultStylesJson
                };

                _context.UserStyles.Add(userStyle);
                await _context.SaveChangesAsync();
            }

            var token = _jwtService.GenerateToken(user.Username, user.Roles);

            var script = $@"
        <script>
            window.opener.postMessage({{
                token: '{token}',
                user: {{
                    idUser: {user.IdUser},
                    username: '{user.Username}',
                    email: '{user.Email}',
                    phone: '{user.Phone}',
                    roles: '{user.Roles}',
                    styleId: '{user.StyleId}'
                }}
            }}, '*');
            window.close();
        </script>
    ";
            return Content(script, "text/html");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}