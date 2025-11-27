using Microsoft.AspNetCore.Mvc;
using Mecha.Helpers;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StylesController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;

        public StylesController(SqlConnectionHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        // GET: api/styles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetStyles()
        {
            try
            {
                var sql = "SELECT style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, location, Social FROM style";
                using var reader = await _sqlHelper.ExecuteReaderAsync(sql);

                var styles = new List<object>();
                while (await reader.ReadAsync())
                {
                    styles.Add(new
                    {
                        styleId = reader["style_id"]?.ToString() ?? "",
                        profileAvatar = reader["profile_avatar"] == DBNull.Value ? null : reader["profile_avatar"]?.ToString(),
                        background = reader["background"] == DBNull.Value ? null : reader["background"]?.ToString(),
                        audio = reader["audio"] == DBNull.Value ? null : reader["audio"]?.ToString(),
                        audioImage = reader["AudioImage"] == DBNull.Value ? null : reader["AudioImage"]?.ToString(),
                        audioTitle = reader["AudioTitle"] == DBNull.Value ? null : reader["AudioTitle"]?.ToString(),
                        customCursor = reader["custom_cursor"] == DBNull.Value ? null : reader["custom_cursor"]?.ToString(),
                        description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString(),
                        username = reader["username"] == DBNull.Value ? null : reader["username"]?.ToString(),
                        location = reader["location"] == DBNull.Value ? null : reader["location"]?.ToString(),
                        social = reader["Social"] == DBNull.Value ? null : reader["Social"]?.ToString()
                    });
                }

                return Ok(styles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving styles", error = ex.Message });
            }
        }

        // GET: api/styles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetStyle(string id)
        {
            try
            {
                var sql = "SELECT style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, location, Social FROM style WHERE style_id = @id";
                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@id", id));

                if (!await reader.ReadAsync())
                    return NotFound();

                return Ok(new
                {
                    styleId = reader["style_id"]?.ToString() ?? "",
                    profileAvatar = reader["profile_avatar"] == DBNull.Value ? null : reader["profile_avatar"]?.ToString(),
                    background = reader["background"] == DBNull.Value ? null : reader["background"]?.ToString(),
                    audio = reader["audio"] == DBNull.Value ? null : reader["audio"]?.ToString(),
                    audioImage = reader["AudioImage"] == DBNull.Value ? null : reader["AudioImage"]?.ToString(),
                    audioTitle = reader["AudioTitle"] == DBNull.Value ? null : reader["AudioTitle"]?.ToString(),
                    customCursor = reader["custom_cursor"] == DBNull.Value ? null : reader["custom_cursor"]?.ToString(),
                    description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString(),
                    username = reader["username"] == DBNull.Value ? null : reader["username"]?.ToString(),
                    location = reader["location"] == DBNull.Value ? null : reader["location"]?.ToString(),
                    social = reader["Social"] == DBNull.Value ? null : reader["Social"]?.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving style", error = ex.Message });
            }
        }

        // POST: api/styles
        [HttpPost]
        public async Task<ActionResult<object>> CreateStyle([FromBody] CreateStyleRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.StyleId))
                    return BadRequest(new { message = "StyleId is required" });

                var sql = @"
                    INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, 
                                      custom_cursor, description, username, location, Social)
                    VALUES (@styleId, @profileAvatar, @background, @audio, @audioImage, @audioTitle, 
                            @customCursor, @description, @username, @location, @social)";

                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    _sqlHelper.CreateParameter("@styleId", request.StyleId),
                    _sqlHelper.CreateParameter("@profileAvatar", request.ProfileAvatar),
                    _sqlHelper.CreateParameter("@background", request.Background),
                    _sqlHelper.CreateParameter("@audio", request.Audio),
                    _sqlHelper.CreateParameter("@audioImage", request.AudioImage),
                    _sqlHelper.CreateParameter("@audioTitle", request.AudioTitle),
                    _sqlHelper.CreateParameter("@customCursor", request.CustomCursor),
                    _sqlHelper.CreateParameter("@description", request.Description),
                    _sqlHelper.CreateParameter("@username", request.Username),
                    _sqlHelper.CreateParameter("@location", request.Location),
                    _sqlHelper.CreateParameter("@social", request.Social));

                return CreatedAtAction(nameof(GetStyle), new { id = request.StyleId }, request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating style", error = ex.Message });
            }
        }

        // PUT: api/styles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStyle(string id, [FromBody] UpdateStyleRequest request)
        {
            try
            {
                if (id != request.StyleId)
                    return BadRequest(new { message = "StyleId mismatch" });

                // Check if style exists
                var checkSql = "SELECT COUNT(*) FROM style WHERE style_id = @id";
                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@id", id))) > 0;

                if (!exists)
                    return NotFound();

                var sql = @"
                    UPDATE style 
                    SET profile_avatar = @profileAvatar, background = @background, audio = @audio,
                        AudioImage = @audioImage, AudioTitle = @audioTitle, custom_cursor = @customCursor,
                        description = @description, username = @username, location = @location, Social = @social
                    WHERE style_id = @styleId";

                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    _sqlHelper.CreateParameter("@styleId", id),
                    _sqlHelper.CreateParameter("@profileAvatar", request.ProfileAvatar),
                    _sqlHelper.CreateParameter("@background", request.Background),
                    _sqlHelper.CreateParameter("@audio", request.Audio),
                    _sqlHelper.CreateParameter("@audioImage", request.AudioImage),
                    _sqlHelper.CreateParameter("@audioTitle", request.AudioTitle),
                    _sqlHelper.CreateParameter("@customCursor", request.CustomCursor),
                    _sqlHelper.CreateParameter("@description", request.Description),
                    _sqlHelper.CreateParameter("@username", request.Username),
                    _sqlHelper.CreateParameter("@location", request.Location),
                    _sqlHelper.CreateParameter("@social", request.Social));

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating style", error = ex.Message });
            }
        }

        // DELETE: api/styles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStyle(string id)
        {
            try
            {
                var checkSql = "SELECT COUNT(*) FROM style WHERE style_id = @id";
                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@id", id))) > 0;

                if (!exists)
                    return NotFound();

                var sql = "DELETE FROM style WHERE style_id = @id";
                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    _sqlHelper.CreateParameter("@id", id));

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting style", error = ex.Message });
            }
        }
    }

    public class CreateStyleRequest
    {
        public string StyleId { get; set; } = "";
        public string? ProfileAvatar { get; set; }
        public string? Background { get; set; }
        public string? Audio { get; set; }
        public string? AudioImage { get; set; }
        public string? AudioTitle { get; set; }
        public string? CustomCursor { get; set; }
        public string? Description { get; set; }
        public string? Username { get; set; }
        public string? Location { get; set; }
        public string? Social { get; set; }
    }

    public class UpdateStyleRequest
    {
        public string StyleId { get; set; } = "";
        public string? ProfileAvatar { get; set; }
        public string? Background { get; set; }
        public string? Audio { get; set; }
        public string? AudioImage { get; set; }
        public string? AudioTitle { get; set; }
        public string? CustomCursor { get; set; }
        public string? Description { get; set; }
        public string? Username { get; set; }
        public string? Location { get; set; }
        public string? Social { get; set; }
    }
}