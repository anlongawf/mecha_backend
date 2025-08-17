using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.DTO;
using System.Data;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/profile/{username}")]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetProfile(string username)
        {
            var user = _context.Users
                .Include(u => u.Style) // Lấy luôn style
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.Style == null)
                return Ok(new { username = user.Username, email = user.Email, style = (object?)null });

            return Ok(new
            {
                styleId = user.Style.StyleId,
                profileAvatar = user.Style.ProfileAvatar,
                background = user.Style.Background,
                audio = user.Style.Audio,
                audioImage = user.Style.AudioImage,
                audioTitle = user.Style.AudioTitle,
                customCursor = user.Style.CustomCursor,
                description = user.Style.Description,
                username = user.Style.Username,
                effectUsername = user.Style.EffectUsername,
                location = user.Style.Location
            });


        }

        // POST: api/profile/{username}
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string username, [FromBody] UpdateProfileDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid body" });

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username is required" });

            try
            {
                // Check if user exists using raw SQL
                var userSql = "SELECT IdUser, Username, StyleId FROM users WHERE Username = @username";
                using var userCommand = _context.Database.GetDbConnection().CreateCommand();
                userCommand.CommandText = userSql;
                var userParam = userCommand.CreateParameter();
                userParam.ParameterName = "@username";
                userParam.Value = username;
                userCommand.Parameters.Add(userParam);

                await _context.Database.OpenConnectionAsync();
                
                int? userId = null;
                string? currentStyleId = null;
                
                using (var reader = await userCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        userId = Convert.ToInt32(reader["IdUser"]);
                        currentStyleId = reader["StyleId"]?.ToString();
                    }
                }

                if (userId == null)
                    return NotFound(new { message = "User not found" });

                string styleId;
                
                if (string.IsNullOrEmpty(currentStyleId))
                {
                    // Create new style
                    styleId = Guid.NewGuid().ToString();
                    
                    var insertStyleSql = @"
                        INSERT INTO style (style_id, profile_avatar, background, audio, audioimage, audiotitle, custom_cursor, description, username, effect_username, location)
                        VALUES (@styleId, @profileAvatar, @background, @audio, @audioImage, @audioTitle, @customCursor, @description, @username, @effectUsername, @location)";
                                        
                    using var insertCommand = _context.Database.GetDbConnection().CreateCommand();
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@styleId", styleId));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@profileAvatar", dto.ProfileAvatar));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@background", dto.Background));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audio", dto.Audio));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audioImage", dto.audioimage)); 
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audioTitle", dto.audiotitle)); 
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@customCursor", dto.CustomCursor));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@description", dto.Description));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@username", dto.Username ?? username));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@effectUsername", dto.EffectUsername));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@location", dto.Location));
                    
                    await insertCommand.ExecuteNonQueryAsync();
                    
                    // Update user's StyleId
                    var updateUserSql = "UPDATE users SET StyleId = @styleId WHERE IdUser = @userId";
                    using var updateUserCommand = _context.Database.GetDbConnection().CreateCommand();
                    updateUserCommand.CommandText = updateUserSql;
                    updateUserCommand.Parameters.Add(CreateParameter(updateUserCommand, "@styleId", styleId));
                    updateUserCommand.Parameters.Add(CreateParameter(updateUserCommand, "@userId", userId.Value));
                    
                    await updateUserCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Update existing style
                    styleId = currentStyleId;
                    
                    var updateParts = new List<string>();
                    var parameters = new List<IDbDataParameter>();
                    
                    using var updateCommand = _context.Database.GetDbConnection().CreateCommand();
                    
                    if (dto.ProfileAvatar != null)
                    {
                        updateParts.Add("profile_avatar = @profileAvatar");
                        parameters.Add(CreateParameter(updateCommand, "@profileAvatar", dto.ProfileAvatar));
                    }
                    if (dto.Background != null)
                    {
                        updateParts.Add("background = @background");
                        parameters.Add(CreateParameter(updateCommand, "@background", dto.Background));
                    }
                    if (dto.Audio != null)
                    {
                        updateParts.Add("audio = @audio");
                        parameters.Add(CreateParameter(updateCommand, "@audio", dto.Audio));
                    }
                    if (dto.CustomCursor != null)
                    {
                        updateParts.Add("custom_cursor = @customCursor");
                        parameters.Add(CreateParameter(updateCommand, "@customCursor", dto.CustomCursor));
                    }
                    if (dto.Description != null)
                    {
                        updateParts.Add("description = @description");
                        parameters.Add(CreateParameter(updateCommand, "@description", dto.Description));
                    }
                    if (dto.Username != null)
                    {
                        updateParts.Add("username = @username");
                        parameters.Add(CreateParameter(updateCommand, "@username", dto.Username));
                    }
                    if (dto.EffectUsername != null)
                    {
                        updateParts.Add("effect_username = @effectUsername");
                        parameters.Add(CreateParameter(updateCommand, "@effectUsername", dto.EffectUsername));
                    }
                    if (dto.Location != null)
                    {
                        updateParts.Add("location = @location");
                        parameters.Add(CreateParameter(updateCommand, "@location", dto.Location));
                    }
                    
                    if (dto.audioimage != null)
                    {
                        updateParts.Add("audioimage = @audioImage");
                        parameters.Add(CreateParameter(updateCommand, "@audioImage", dto.audioimage));
                    }

                    if (dto.audiotitle != null)
                    {
                        updateParts.Add("audiotitle = @audioTitle");
                        parameters.Add(CreateParameter(updateCommand, "@audioTitle", dto.audiotitle));
                    }

                    
                    if (updateParts.Any())
                    {
                        var updateStyleSql = $"UPDATE style SET {string.Join(", ", updateParts)} WHERE style_id = @styleId";
                        updateCommand.CommandText = updateStyleSql;
                        parameters.Add(CreateParameter(updateCommand, "@styleId", styleId));
                        
                        foreach (var param in parameters)
                        {
                            updateCommand.Parameters.Add(param);
                        }
                        
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }

                // Get the updated style data
                var getUpdatedStyleSql = @"
                        SELECT style_id, profile_avatar, background, audio, audioimage, audiotitle, custom_cursor, description, username, effect_username, location
                        FROM style WHERE style_id = @styleId";

                
                using var getStyleCommand = _context.Database.GetDbConnection().CreateCommand();
                getStyleCommand.CommandText = getUpdatedStyleSql;
                getStyleCommand.Parameters.Add(CreateParameter(getStyleCommand, "@styleId", styleId));
                
                using var styleReader = await getStyleCommand.ExecuteReaderAsync();
                
                if (await styleReader.ReadAsync())
                {
                    var updatedStyle = new
                    {
                        styleId = styleReader["style_id"]?.ToString() ?? "",
                        profileAvatar = styleReader["profile_avatar"]?.ToString() ?? "",
                        background = styleReader["background"]?.ToString() ?? "",
                        audio = styleReader["audio"]?.ToString() ?? "",
                        audioImage = styleReader["audioimage"]?.ToString() ?? "", // camelCase
                        audioTitle = styleReader["audiotitle"]?.ToString() ?? "",  // camelCase
                        customCursor = styleReader["custom_cursor"]?.ToString() ?? "",
                        description = styleReader["description"]?.ToString() ?? "",
                        username = styleReader["username"]?.ToString() ?? "",
                        effectUsername = styleReader["effect_username"]?.ToString() ?? "",
                        location = styleReader["location"]?.ToString() ?? ""
                    };
                    
                    return Ok(new
                    {
                        message = "Profile updated successfully",
                        Style = updatedStyle
                    });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to retrieve updated style" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the profile", error = ex.Message });
            }
        }
        
        private IDbDataParameter CreateParameter(IDbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }
    }
}