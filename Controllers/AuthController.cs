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

            // Tạo user mới
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Phone = request.Phone,
                PassWords = hashedPassword,
                Roles = "user"
                // Không cần gán StyleId nếu IdUser là auto-increment
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Lấy IdUser vừa tạo (auto-increment)
            int newUserId = user.IdUser;

            // Tạo record UserStyles mặc định
            var userStyles = new UserStyle
            {
                IdUser = newUserId,
                Styles = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "background", "#ffffff" },
                    { "profileAvatar", "" },
                    { "audio", "" },
                    { "customCursor", "" },
                    { "description", "" },
                    { "location", "" },
                    { "audioImage", "" },
                    { "audioTitle", "" }
                })
            };

            _context.UserStyles.Add(userStyles);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully", userId = newUserId });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !VerifyPassword(request.Password, user.PassWords))
                return Unauthorized("Invalid username or password");

            var token = _jwtService.GenerateToken(user.Username, user.Roles);

            return Ok(new
            {
                token,
                user = new { user.IdUser, user.Username, user.Email, user.Phone, user.Roles }
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
}
