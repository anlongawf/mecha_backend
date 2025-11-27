using Microsoft.AspNetCore.Mvc;
using Mecha.DTO;
using Mecha.Helpers;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserStylesController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;

        public UserStylesController(SqlConnectionHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }
        
        // GET: api/UserStyles/username/{username}
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUserStyleByUsername(string username)
        {
            try
            {
                var sql = @"
                    SELECT style_id, profile_avatar, background, audio, custom_cursor, description, location, AudioImage, AudioTitle
                    FROM style
                    WHERE username = @username";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@username", username));

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Style not found for this user" });

                var dto = new
                {
                    StyleId = reader["style_id"]?.ToString(),
                    ProfileAvatar = reader["profile_avatar"] == DBNull.Value ? null : reader["profile_avatar"]?.ToString(),
                    Background = reader["background"] == DBNull.Value ? null : reader["background"]?.ToString(),
                    Audio = reader["audio"] == DBNull.Value ? null : reader["audio"]?.ToString(),
                    CustomCursor = reader["custom_cursor"] == DBNull.Value ? null : reader["custom_cursor"]?.ToString(),
                    Description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString(),
                    Location = reader["location"] == DBNull.Value ? null : reader["location"]?.ToString(),
                    AudioImage = reader["AudioImage"] == DBNull.Value ? null : reader["AudioImage"]?.ToString(),
                    AudioTitle = reader["AudioTitle"] == DBNull.Value ? null : reader["AudioTitle"]?.ToString()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving style", error = ex.Message });
            }
        }

        // GET: api/UserStyles/19
        [HttpGet("{idUser}")]
        public async Task<IActionResult> GetUserStyle(int idUser)
        {
            try
            {
                var sql = "SELECT idUser, styles FROM user_styles WHERE idUser = @idUser";
                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@idUser", idUser));

                if (!await reader.ReadAsync())
                    return NotFound();

                var stylesJson = reader["styles"]?.ToString();
                var styles = JsonSerializer.Deserialize<Dictionary<string, object>>(stylesJson);

                var dto = new UserStyleDto
                {
                    IdUser = Convert.ToInt32(reader["idUser"]),
                    Styles = styles ?? new Dictionary<string, object>()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user style", error = ex.Message });
            }
        }

        // POST: api/UserStyles
        [HttpPost]
        public async Task<IActionResult> CreateUserStyle([FromBody] UserStyleDto dto)
        {
            try
            {
                if (dto == null) return BadRequest("Invalid body");

                var stylesJson = JsonSerializer.Serialize(dto.Styles);
                var sql = "INSERT INTO user_styles (idUser, styles) VALUES (@idUser, @styles)";

                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    _sqlHelper.CreateParameter("@idUser", dto.IdUser),
                    _sqlHelper.CreateParameter("@styles", stylesJson));

                return Ok(new { idUser = dto.IdUser, styles = dto.Styles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating user style", error = ex.Message });
            }
        }

        // PUT: api/UserStyles/19
        [HttpPut("{idUser}")]
        public async Task<IActionResult> UpdateUserStyle(int idUser, [FromBody] UserStyleDto dto)
        {
            try
            {
                // Check if exists
                var checkSql = "SELECT COUNT(*) FROM user_styles WHERE idUser = @idUser";
                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@idUser", idUser))) > 0;

                if (!exists)
                    return NotFound();

                var stylesJson = JsonSerializer.Serialize(dto.Styles);
                var sql = "UPDATE user_styles SET styles = @styles WHERE idUser = @idUser";

                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    _sqlHelper.CreateParameter("@styles", stylesJson),
                    _sqlHelper.CreateParameter("@idUser", idUser));

                return Ok(new { idUser = idUser, styles = dto.Styles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating user style", error = ex.Message });
            }
        }

        // DELETE: api/UserStyles/19
        [HttpDelete("{idUser}")]
        public async Task<IActionResult> DeleteUserStyle(int idUser)
        {
            try
            {
                var checkSql = "SELECT COUNT(*) FROM user_styles WHERE idUser = @idUser";
                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@idUser", idUser))) > 0;

                if (!exists)
                    return NotFound();

                var sql = "DELETE FROM user_styles WHERE idUser = @idUser";
                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    _sqlHelper.CreateParameter("@idUser", idUser));

                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting user style", error = ex.Message });
            }
        }
    }
}
