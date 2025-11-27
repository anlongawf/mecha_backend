using Microsoft.AspNetCore.Mvc;
using Mecha.Helpers;
using Mecha.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;
        private readonly JwtService _jwtService;

        public AuthController(SqlConnectionHelper sqlHelper, JwtService jwtService)
        {
            _sqlHelper = sqlHelper;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                // Kiểm tra username hoặc email đã tồn tại
                var checkSql = "SELECT COUNT(*) FROM users WHERE Username = @username OR Email = @email";
                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@username", request.Username),
                    _sqlHelper.CreateParameter("@email", request.Email))) > 0;

                if (exists)
                    return BadRequest("Username or Email already exists");

                // Hash password
                var hashedPassword = HashPassword(request.Password);

                // Tạo StyleModel mặc định trước
                var defaultStyleId = Guid.NewGuid().ToString();
                var insertStyleSql = @"
                    INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, 
                                      custom_cursor, description, username, location, Social)
                    VALUES (@styleId, NULL, NULL, NULL, NULL, NULL, NULL, @description, @username, NULL, NULL)";

                await _sqlHelper.ExecuteNonQueryAsync(insertStyleSql,
                    _sqlHelper.CreateParameter("@styleId", defaultStyleId),
                    _sqlHelper.CreateParameter("@description", "Welcome to Mecha!"),
                    _sqlHelper.CreateParameter("@username", request.Username));

                // Tạo user mới với StyleId và Premium = 0 (non-premium)
                var insertUserSql = @"
                    INSERT INTO users (Username, Email, Phone, password, Roles, StyleId, Premium, IsVerified, CreatedAt)
                    VALUES (@username, @email, @phone, @password, @roles, @styleId, @premium, @isVerified, @createdAt)";

                await _sqlHelper.ExecuteNonQueryAsync(insertUserSql,
                    _sqlHelper.CreateParameter("@username", request.Username),
                    _sqlHelper.CreateParameter("@email", request.Email),
                    _sqlHelper.CreateParameter("@phone", request.Phone),
                    _sqlHelper.CreateParameter("@password", hashedPassword),
                    _sqlHelper.CreateParameter("@roles", "user"),
                    _sqlHelper.CreateParameter("@styleId", defaultStyleId),
                    _sqlHelper.CreateParameter("@premium", false),
                    _sqlHelper.CreateParameter("@isVerified", false),
                    _sqlHelper.CreateParameter("@createdAt", DateTime.UtcNow));

                // Lấy IdUser vừa tạo
                var getUserIdSql = "SELECT IdUser FROM users WHERE Username = @username";
                var userId = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(getUserIdSql,
                    _sqlHelper.CreateParameter("@username", request.Username)));

                // Tạo JSON object mặc định cho UserStyle
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
                var insertUserStyleSql = "INSERT INTO user_styles (idUser, styles) VALUES (@idUser, @styles)";
                await _sqlHelper.ExecuteNonQueryAsync(insertUserStyleSql,
                    _sqlHelper.CreateParameter("@idUser", userId),
                    _sqlHelper.CreateParameter("@styles", defaultStylesJson));

                return Ok(new { 
                    message = "User registered successfully", 
                    userId = userId,
                    styleId = defaultStyleId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error registering user", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var sql = @"
                    SELECT IdUser, Username, Email, Phone, Roles, StyleId, Premium, password
                    FROM users 
                    WHERE Username = @username";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@username", request.Username));

                if (!await reader.ReadAsync())
                    return Unauthorized("Invalid username or password");

                var storedHash = reader["password"] == DBNull.Value ? null : reader["password"]?.ToString();
                if (storedHash == null || !VerifyPassword(request.Password, storedHash))
                    return Unauthorized("Invalid username or password");

                var userId = Convert.ToInt32(reader["IdUser"]);
                var username = reader["Username"]?.ToString();
                var email = reader["Email"] == DBNull.Value ? null : reader["Email"]?.ToString();
                var phone = reader["Phone"] == DBNull.Value ? null : reader["Phone"]?.ToString();
                var roles = reader["Roles"] == DBNull.Value ? "user" : reader["Roles"]?.ToString();
                var styleId = reader["StyleId"] == DBNull.Value ? null : reader["StyleId"]?.ToString();
                var premium = Convert.ToBoolean(reader["Premium"]);

                var token = _jwtService.GenerateToken(username, roles);

                return Ok(new
                {
                    token,
                    user = new 
                    { 
                        IdUser = userId,
                        Username = username, 
                        Email = email, 
                        Phone = phone, 
                        Roles = roles,
                        StyleId = styleId,
                        Premium = premium
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error during login", error = ex.Message });
            }
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