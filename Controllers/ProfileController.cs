using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.Models;
using Mecha.DTO;
using System.Data;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpPut("{id}/change-username")]
        public async Task<IActionResult> ChangeUsername(int id, [FromBody] string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
                return BadRequest(new { message = "Username cannot be empty" });

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Kiểm tra username mới đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == newUsername))
                return Conflict(new { message = "Username already exists" });

            user.Username = newUsername;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Username updated successfully", username = newUsername });
        }

        // GET: api/profile/username/{username}
        [HttpGet("username/{username}")]
        public IActionResult GetProfileByUsername(string username)
        {
            var user = _context.Users
                .Include(u => u.Style) // Lấy luôn style
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.Style == null)
                return Ok(new { 
                    userId = user.IdUser,
                    username = user.Username, 
                    email = user.Email, 
                    style = (object?)null 
                });

            return Ok(new
            {
                userId = user.IdUser,
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
        
        // GET: api/profile/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfileById(int id)
        {
            var user = await _context.Users
                .Include(u => u.Style)
                .FirstOrDefaultAsync(u => u.IdUser == id);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.Style == null)
                return Ok(new { 
                    userId = user.IdUser,
                    username = user.Username, 
                    email = user.Email, 
                    style = (object?)null 
                });

            return Ok(new
            {
                userId = user.IdUser,
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

        // POST: api/profile/{id}
        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateProfileById(int id, [FromBody] UpdateProfileDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid body" });

            try
            {
                // Check if user exists using raw SQL
                var userSql = "SELECT IdUser, Username, StyleId FROM users WHERE IdUser = @userId";
                using var userCommand = _context.Database.GetDbConnection().CreateCommand();
                userCommand.CommandText = userSql;
                var userParam = userCommand.CreateParameter();
                userParam.ParameterName = "@userId";
                userParam.Value = id;
                userCommand.Parameters.Add(userParam);

                await _context.Database.OpenConnectionAsync();
                
                string? currentStyleId = null;
                string? currentUsername = null;
                
                using (var reader = await userCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        currentStyleId = reader["StyleId"]?.ToString();
                        currentUsername = reader["Username"]?.ToString();
                    }
                }

                if (currentUsername == null)
                    return NotFound(new { message = "User not found" });

                // Handle username change if provided and different
                string updatedUsername = currentUsername;
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != currentUsername)
                {
                    // Check if new username already exists
                    var checkUsernameSql = "SELECT COUNT(*) FROM users WHERE Username = @newUsername AND IdUser != @userId";
                    using var checkCommand = _context.Database.GetDbConnection().CreateCommand();
                    checkCommand.CommandText = checkUsernameSql;
                    checkCommand.Parameters.Add(CreateParameter(checkCommand, "@newUsername", dto.Username));
                    checkCommand.Parameters.Add(CreateParameter(checkCommand, "@userId", id));
                    
                    var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (existingCount > 0)
                        return Conflict(new { message = "Username already exists" });

                    // Update username in users table
                    var updateUsernameSql = "UPDATE users SET Username = @newUsername WHERE IdUser = @userId";
                    using var updateUsernameCommand = _context.Database.GetDbConnection().CreateCommand();
                    updateUsernameCommand.CommandText = updateUsernameSql;
                    updateUsernameCommand.Parameters.Add(CreateParameter(updateUsernameCommand, "@newUsername", dto.Username));
                    updateUsernameCommand.Parameters.Add(CreateParameter(updateUsernameCommand, "@userId", id));
                    
                    await updateUsernameCommand.ExecuteNonQueryAsync();
                    updatedUsername = dto.Username;
                }

                string styleId;
                
                if (string.IsNullOrEmpty(currentStyleId))
                {
                    // Create new style
                    styleId = Guid.NewGuid().ToString();
                    
                    var insertStyleSql = @"
                        INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, effect_username, location)
                        VALUES (@styleId, @profileAvatar, @background, @audio, @audioImage, @audioTitle, @customCursor, @description, @username, @effectUsername, @location)";
                                        
                    using var insertCommand = _context.Database.GetDbConnection().CreateCommand();
                    insertCommand.CommandText = insertStyleSql;
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@styleId", styleId));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@profileAvatar", dto.ProfileAvatar));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@background", dto.Background));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audio", dto.Audio));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audioImage", dto.AudioImage)); 
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audioTitle", dto.AudioTitle)); 
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@customCursor", dto.CustomCursor));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@description", dto.Description));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@username", updatedUsername)); // Use updated username
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@effectUsername", dto.EffectUsername));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@location", dto.Location));
                    
                    await insertCommand.ExecuteNonQueryAsync();
                    
                    // Update user's StyleId
                    var updateUserSql = "UPDATE users SET StyleId = @styleId WHERE IdUser = @userId";
                    using var updateUserCommand = _context.Database.GetDbConnection().CreateCommand();
                    updateUserCommand.CommandText = updateUserSql;
                    updateUserCommand.Parameters.Add(CreateParameter(updateUserCommand, "@styleId", styleId));
                    updateUserCommand.Parameters.Add(CreateParameter(updateUserCommand, "@userId", id));
                    
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
                        parameters.Add(CreateParameter(updateCommand, "@username", updatedUsername)); // Use updated username
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
                    
                    if (dto.AudioImage != null)
                    {
                        updateParts.Add("AudioImage = @audioImage");
                        parameters.Add(CreateParameter(updateCommand, "@audioImage", dto.AudioImage));
                    }

                    if (dto.AudioTitle != null)
                    {
                        updateParts.Add("AudioTitle = @audioTitle");
                        parameters.Add(CreateParameter(updateCommand, "@audioTitle", dto.AudioTitle));
                    }

                    // Always update style username if username changed
                    if (updatedUsername != currentUsername)
                    {
                        updateParts.Add("username = @styleUsername");
                        parameters.Add(CreateParameter(updateCommand, "@styleUsername", updatedUsername));
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
                        SELECT style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, effect_username, location
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
                        audioImage = styleReader["AudioImage"]?.ToString() ?? "", 
                        audioTitle = styleReader["AudioTitle"]?.ToString() ?? "",  
                        customCursor = styleReader["custom_cursor"]?.ToString() ?? "",
                        description = styleReader["description"]?.ToString() ?? "",
                        username = styleReader["username"]?.ToString() ?? "",
                        effectUsername = styleReader["effect_username"]?.ToString() ?? "",
                        location = styleReader["location"]?.ToString() ?? ""
                    };
                    
                    return Ok(new
                    {
                        message = "Profile updated successfully",
                        Style = updatedStyle,
                        newUsername = updatedUsername != currentUsername ? updatedUsername : null // Include new username if changed
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
            finally
            {
                if (_context.Database.GetDbConnection().State == ConnectionState.Open)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }

        // POST: api/profile/username/{username} - Keep old endpoint for backward compatibility
        [HttpPost("username/{username}")]
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
                string currentUsername = username;
                
                using (var reader = await userCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        userId = Convert.ToInt32(reader["IdUser"]);
                        currentStyleId = reader["StyleId"]?.ToString();
                        currentUsername = reader["Username"]?.ToString() ?? username;
                    }
                }

                if (userId == null)
                    return NotFound(new { message = "User not found" });

                // Handle username change if provided and different
                string updatedUsername = currentUsername;
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != currentUsername)
                {
                    // Check if new username already exists
                    var checkUsernameSql = "SELECT COUNT(*) FROM users WHERE Username = @newUsername";
                    using var checkCommand = _context.Database.GetDbConnection().CreateCommand();
                    checkCommand.CommandText = checkUsernameSql;
                    checkCommand.Parameters.Add(CreateParameter(checkCommand, "@newUsername", dto.Username));
                    
                    var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (existingCount > 0)
                        return Conflict(new { message = "Username already exists" });

                    // Update username in users table
                    var updateUsernameSql = "UPDATE users SET Username = @newUsername WHERE IdUser = @userId";
                    using var updateUsernameCommand = _context.Database.GetDbConnection().CreateCommand();
                    updateUsernameCommand.CommandText = updateUsernameSql;
                    updateUsernameCommand.Parameters.Add(CreateParameter(updateUsernameCommand, "@newUsername", dto.Username));
                    updateUsernameCommand.Parameters.Add(CreateParameter(updateUsernameCommand, "@userId", userId.Value));
                    
                    await updateUsernameCommand.ExecuteNonQueryAsync();
                    updatedUsername = dto.Username;
                }

                string styleId;
                
                if (string.IsNullOrEmpty(currentStyleId))
                {
                    // Create new style
                    styleId = Guid.NewGuid().ToString();
                    
                    var insertStyleSql = @"
                        INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, effect_username, location)
                        VALUES (@styleId, @profileAvatar, @background, @audio, @audioImage, @audioTitle, @customCursor, @description, @username, @effectUsername, @location)";
                                        
                    using var insertCommand = _context.Database.GetDbConnection().CreateCommand();
                    insertCommand.CommandText = insertStyleSql;
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@styleId", styleId));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@profileAvatar", dto.ProfileAvatar));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@background", dto.Background));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audio", dto.Audio));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audioImage", dto.AudioImage)); 
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@audioTitle", dto.AudioTitle)); 
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@customCursor", dto.CustomCursor));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@description", dto.Description));
                    insertCommand.Parameters.Add(CreateParameter(insertCommand, "@username", updatedUsername)); // Use updated username
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
                        parameters.Add(CreateParameter(updateCommand, "@username", updatedUsername)); // Use updated username
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
                    
                    if (dto.AudioImage != null)
                    {
                        updateParts.Add("AudioImage = @audioImage");
                        parameters.Add(CreateParameter(updateCommand, "@audioImage", dto.AudioImage));
                    }

                    if (dto.AudioTitle != null)
                    {
                        updateParts.Add("AudioTitle = @audioTitle");
                        parameters.Add(CreateParameter(updateCommand, "@audioTitle", dto.AudioTitle));
                    }

                    // Always update style username if username changed
                    if (updatedUsername != currentUsername)
                    {
                        updateParts.Add("username = @styleUsername");
                        parameters.Add(CreateParameter(updateCommand, "@styleUsername", updatedUsername));
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
                        SELECT style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, effect_username, location
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
                        audioImage = styleReader["AudioImage"]?.ToString() ?? "", 
                        audioTitle = styleReader["AudioTitle"]?.ToString() ?? "",  
                        customCursor = styleReader["custom_cursor"]?.ToString() ?? "",
                        description = styleReader["description"]?.ToString() ?? "",
                        username = styleReader["username"]?.ToString() ?? "",
                        effectUsername = styleReader["effect_username"]?.ToString() ?? "",
                        location = styleReader["location"]?.ToString() ?? ""
                    };
                    
                    return Ok(new
                    {
                        message = "Profile updated successfully",
                        Style = updatedStyle,
                        newUsername = updatedUsername != currentUsername ? updatedUsername : null // Include new username if changed
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
            finally
            {
                if (_context.Database.GetDbConnection().State == ConnectionState.Open)
                {
                    await _context.Database.CloseConnectionAsync();
                }
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