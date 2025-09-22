using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Kiểm tra username hoặc email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
                return BadRequest("Username or Email already exists");

            // Hash password
            var hashedPassword = HashPassword(request.Password);

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
                Username = request.Username,
                Location = null,
                AudioImage = null,
                AudioTitle = null,
                Social = null
            };

            _context.Styles.Add(defaultStyleModel);

            // Tạo user mới với StyleId và Premium = 0 (non-premium)
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Phone = request.Phone,
                password = hashedPassword,
                Roles = "user",
                StyleId = defaultStyleId,
                Premium = false,
                IsVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Lưu để có IdUser

            // Tạo JSON object mặc định cho UserStyle (giống với Discord auth)
            var defaultStylesJson = JsonSerializer.Serialize(new
            {
                theme = "default",
                color_scheme = "light",
                layout = "standard",
                custom_css = "",
                background = "#ffffff",
                profileAvatar = "",
                audio = "",
                customCursor = "",
                description = "Welcome to Mecha!",
                location = "",
                audioImage = "",
                audioTitle = "",
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

            return Ok(new { 
                message = "User registered successfully", 
                userId = user.IdUser,
                styleId = defaultStyleId
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            
            if (user == null || !VerifyPassword(request.Password, user.password))
                return Unauthorized("Invalid username or password");

            var token = _jwtService.GenerateToken(user.Username, user.Roles);

            return Ok(new
            {
                token,
                user = new 
                { 
                    user.IdUser, 
                    user.Username, 
                    user.Email, 
                    user.Phone, 
                    user.Roles,
                    user.StyleId,
                    user.Premium 
                }
            });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }
    }

    // Request models
    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}