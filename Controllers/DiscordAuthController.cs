using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using AspNet.Security.OAuth.Discord;
using System.Security.Claims;
using Mecha.Helpers;
using Mecha.Services;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscordAuthController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;
        private readonly JwtService _jwtService;

        public DiscordAuthController(SqlConnectionHelper sqlHelper, JwtService jwtService)
        {
            _sqlHelper = sqlHelper;
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

            try
            {
                // Check if user exists
                var checkUserSql = "SELECT IdUser, Username, Email, Phone, Roles, StyleId FROM users WHERE DiscordId = @discordId";
                using var userReader = await _sqlHelper.ExecuteReaderAsync(checkUserSql,
                    _sqlHelper.CreateParameter("@discordId", discordId));

                int userId;
                string userUsername;
                string? userEmail;
                string? userPhone;
                string userRoles;
                string? styleId;

                if (await userReader.ReadAsync())
                {
                    // User exists
                    userId = Convert.ToInt32(userReader["IdUser"]);
                    userUsername = userReader["Username"]?.ToString() ?? "";
                    userEmail = userReader["Email"] == DBNull.Value ? null : userReader["Email"]?.ToString();
                    userPhone = userReader["Phone"] == DBNull.Value ? null : userReader["Phone"]?.ToString();
                    userRoles = userReader["Roles"] == DBNull.Value ? "user" : userReader["Roles"]?.ToString() ?? "user";
                    styleId = userReader["StyleId"] == DBNull.Value ? null : userReader["StyleId"]?.ToString();
                }
                else
                {
                    // Create new user
                    var defaultStyleId = Guid.NewGuid().ToString();
                    var finalUsername = username ?? $"discord_{discordId}";
                    var finalEmail = email ?? "";

                    // Insert style
                    var insertStyleSql = @"
                        INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, 
                                          custom_cursor, description, username, location, Social)
                        VALUES (@styleId, NULL, NULL, NULL, NULL, NULL, NULL, @description, @username, NULL, NULL)";

                    await _sqlHelper.ExecuteNonQueryAsync(insertStyleSql,
                        _sqlHelper.CreateParameter("@styleId", defaultStyleId),
                        _sqlHelper.CreateParameter("@description", "Welcome to Mecha!"),
                        _sqlHelper.CreateParameter("@username", finalUsername));

                    // Insert user
                    var insertUserSql = @"
                        INSERT INTO users (Username, Email, DiscordId, password, Roles, Phone, StyleId, Premium, IsVerified, CreatedAt)
                        VALUES (@username, @email, @discordId, @password, @roles, @phone, @styleId, @premium, @isVerified, @createdAt)";

                    await _sqlHelper.ExecuteNonQueryAsync(insertUserSql,
                        _sqlHelper.CreateParameter("@username", finalUsername),
                        _sqlHelper.CreateParameter("@email", finalEmail),
                        _sqlHelper.CreateParameter("@discordId", discordId),
                        _sqlHelper.CreateParameter("@password", Guid.NewGuid().ToString()),
                        _sqlHelper.CreateParameter("@roles", "user"),
                        _sqlHelper.CreateParameter("@phone", ""),
                        _sqlHelper.CreateParameter("@styleId", defaultStyleId),
                        _sqlHelper.CreateParameter("@premium", false),
                        _sqlHelper.CreateParameter("@isVerified", false),
                        _sqlHelper.CreateParameter("@createdAt", DateTime.UtcNow));

                    // Get new user ID
                    var getUserIdSql = "SELECT IdUser FROM users WHERE DiscordId = @discordId";
                    userId = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(getUserIdSql,
                        _sqlHelper.CreateParameter("@discordId", discordId)));

                    // Create default UserStyle
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

                    var insertUserStyleSql = "INSERT INTO user_styles (idUser, styles) VALUES (@idUser, @styles)";
                    await _sqlHelper.ExecuteNonQueryAsync(insertUserStyleSql,
                        _sqlHelper.CreateParameter("@idUser", userId),
                        _sqlHelper.CreateParameter("@styles", defaultStylesJson));

                    userUsername = finalUsername;
                    userEmail = finalEmail;
                    userPhone = "";
                    userRoles = "user";
                    styleId = defaultStyleId;
                }

                var token = _jwtService.GenerateToken(userUsername, userRoles);

                var script = $@"
        <script>
            window.opener.postMessage({{
                token: '{token}',
                user: {{
                    idUser: {userId},
                    username: '{userUsername}',
                    email: '{userEmail ?? ""}',
                    phone: '{userPhone ?? ""}',
                    roles: '{userRoles}',
                    styleId: '{styleId ?? ""}'
                }}
            }}, '*');
            window.close();
        </script>
    ";
                return Content(script, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<script>window.opener.postMessage({{ error: 'Error: {ex.Message}' }}, '*'); window.close();</script>", "text/html");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}