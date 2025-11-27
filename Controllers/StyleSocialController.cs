using Microsoft.AspNetCore.Mvc;
using Mecha.DTO;
using Mecha.Helpers;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StyleSocialController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;

        public StyleSocialController(SqlConnectionHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        // GET /api/StyleSocial/byUser/{userId}
        [HttpGet("byUser/{userId}")]
        public async Task<IActionResult> GetSocialByUser(int userId)
        {
            try
            {
                var sql = @"
                    SELECT s.Social
                    FROM users u
                    INNER JOIN style s ON u.StyleId = s.style_id
                    WHERE u.IdUser = @userId";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@userId", userId));

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Style not found for this user" });

                var socialJson = reader["Social"] == DBNull.Value ? null : reader["Social"]?.ToString();
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };
                var socialList = string.IsNullOrEmpty(socialJson)
                    ? new List<SocialDto>()
                    : JsonSerializer.Deserialize<List<SocialDto>>(socialJson, deserializeOptions) ?? new List<SocialDto>();

                // CRITICAL: Ensure all items have Size property (default to 36 if missing or null)
                // This handles cases where Size was not saved in older records
                foreach (var item in socialList)
                {
                    // If Size is null, 0, or not set, default to 36
                    if (!item.Size.HasValue || item.Size.Value == 0)
                    {
                        item.Size = 36;
                    }
                }
                
                // Log for debugging
                Console.WriteLine($"Deserialized {socialList.Count} social links. Sizes: {string.Join(", ", socialList.Select(s => s.Size?.ToString() ?? "null"))}");

                // If no social links exist, return default social links
                if (socialList.Count == 0)
                {
                    socialList = GetDefaultSocialLinks();
                }

                // Return JSON with explicit serialization to ensure Size is included
                // ASP.NET Core's Ok() uses global JSON options which might ignore null values
                var serializeOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    // Ensure null values are written as null, not omitted
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                // Double-check all items have Size before serializing
                foreach (var item in socialList)
                {
                    if (!item.Size.HasValue || item.Size.Value == 0)
                    {
                        item.Size = 36;
                    }
                }
                
                // Serialize manually to ensure all properties including Size are included
                var jsonString = JsonSerializer.Serialize(socialList, serializeOptions);
                Console.WriteLine($"Serialized JSON response: {jsonString}"); // Debug log
                return Content(jsonString, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving social links", error = ex.Message });
            }
        }

        // Default social links with popular platforms
        private List<SocialDto> GetDefaultSocialLinks()
        {
            return new List<SocialDto>
            {
                new SocialDto { Icon = "fab fa-facebook", Url = "", Color = "#1877F2", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-twitter", Url = "", Color = "#1DA1F2", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-instagram", Url = "", Color = "#E4405F", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-github", Url = "", Color = "#181717", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-linkedin", Url = "", Color = "#0A66C2", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-youtube", Url = "", Color = "#FF0000", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-discord", Url = "", Color = "#5865F2", Size = 36, MarginLeft = 0, MarginRight = 0 },
                new SocialDto { Icon = "fab fa-tiktok", Url = "", Color = "#000000", Size = 36, MarginLeft = 0, MarginRight = 0 }
            };
        }

        // PUT /api/StyleSocial/byUser/{userId}
        [HttpPut("byUser/{userId}")]
        public async Task<IActionResult> UpdateSocialByUser(int userId, [FromBody] List<SocialDto> social)
        {
            try
            {
                // Check if user and style exist
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM users u
                    INNER JOIN style s ON u.StyleId = s.style_id
                    WHERE u.IdUser = @userId";

                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@userId", userId))) > 0;

                if (!exists)
                    return NotFound(new { message = "Style not found for this user" });

                // Serialize with options to ensure all properties including Size are included
                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                    WriteIndented = false
                };
                var socialJson = JsonSerializer.Serialize(social, options);
                var updateSql = @"
                    UPDATE style s
                    INNER JOIN users u ON s.style_id = u.StyleId
                    SET s.Social = @social
                    WHERE u.IdUser = @userId";

                await _sqlHelper.ExecuteNonQueryAsync(updateSql,
                    _sqlHelper.CreateParameter("@social", socialJson),
                    _sqlHelper.CreateParameter("@userId", userId));

                return Ok(new { message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating social links", error = ex.Message });
            }
        }
        
        [HttpPost("byUser/{userId}/upload-icon")]
        public async Task<IActionResult> UploadIcon(int userId, [FromForm] IFormFile iconFile)
        {
            try
            {
                if (iconFile == null || iconFile.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                // Check if user and style exist
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM users u
                    INNER JOIN style s ON u.StyleId = s.style_id
                    WHERE u.IdUser = @userId";

                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@userId", userId))) > 0;

                if (!exists)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading icon", error = ex.Message });
            }
        }

    }
}