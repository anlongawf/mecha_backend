using System.Data;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.DTO;
using Mecha.Helpers;

namespace Mecha.Services
{
    public class StyleService
    {
        private readonly AppDbContext _context;
        private readonly DatabaseHelper _dbHelper;

        public StyleService(AppDbContext context, DatabaseHelper dbHelper)
        {
            _context = context;
            _dbHelper = dbHelper;
        }

        public async Task<string> CreateNewStyleAsync(UpdateProfileDto dto, string username)
        {
            var styleId = Guid.NewGuid().ToString();
            
            var insertStyleSql = @"
                INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, location)
                VALUES (@styleId, @profileAvatar, @background, @audio, @audioImage, @audioTitle, @customCursor, @description, @username, @location)";
                                
            using var insertCommand = _context.Database.GetDbConnection().CreateCommand();
            insertCommand.CommandText = insertStyleSql;
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@styleId", styleId));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@profileAvatar", dto.ProfileAvatar));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@background", dto.Background));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@audio", dto.Audio));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@audioImage", dto.AudioImage));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@audioTitle", dto.AudioTitle));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@customCursor", dto.CustomCursor));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@description", dto.Description));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@username", username));
            insertCommand.Parameters.Add(_dbHelper.CreateParameter(insertCommand, "@location", dto.Location));
            
            await insertCommand.ExecuteNonQueryAsync();
            return styleId;
        }

        public async Task UpdateUserStyleIdAsync(int userId, string styleId)
        {
            var updateUserSql = "UPDATE users SET StyleId = @styleId WHERE IdUser = @userId";
            using var updateUserCommand = _context.Database.GetDbConnection().CreateCommand();
            updateUserCommand.CommandText = updateUserSql;
            updateUserCommand.Parameters.Add(_dbHelper.CreateParameter(updateUserCommand, "@styleId", styleId));
            updateUserCommand.Parameters.Add(_dbHelper.CreateParameter(updateUserCommand, "@userId", userId));
            
            await updateUserCommand.ExecuteNonQueryAsync();
        }

        public async Task UpdateExistingStyleAsync(string styleId, UpdateProfileDto dto, string? updatedUsername = null, string? currentUsername = null)
        {
            var updateParts = new List<string>();
            var parameters = new List<IDbDataParameter>();
            
            using var updateCommand = _context.Database.GetDbConnection().CreateCommand();
            
            if (dto.ProfileAvatar != null)
            {
                updateParts.Add("profile_avatar = @profileAvatar");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@profileAvatar", dto.ProfileAvatar));
            }
            if (dto.Background != null)
            {
                updateParts.Add("background = @background");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@background", dto.Background));
            }
            if (dto.Audio != null)
            {
                updateParts.Add("audio = @audio");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@audio", dto.Audio));
            }
            if (dto.CustomCursor != null)
            {
                updateParts.Add("custom_cursor = @customCursor");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@customCursor", dto.CustomCursor));
            }
            if (dto.Description != null)
            {
                updateParts.Add("description = @description");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@description", dto.Description));
            }
            if (dto.Location != null)
            {
                updateParts.Add("location = @location");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@location", dto.Location));
            }
            if (dto.AudioImage != null)
            {
                updateParts.Add("AudioImage = @audioImage");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@audioImage", dto.AudioImage));
            }
            if (dto.AudioTitle != null)
            {
                updateParts.Add("AudioTitle = @audioTitle");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@audioTitle", dto.AudioTitle));
            }

            // Update username in style if changed
            if (!string.IsNullOrEmpty(updatedUsername) && updatedUsername != currentUsername)
            {
                updateParts.Add("username = @username");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@username", updatedUsername));
            }
            else if (dto.Username != null && string.IsNullOrEmpty(updatedUsername))
            {
                updateParts.Add("username = @username");
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@username", dto.Username));
            }

            if (updateParts.Any())
            {
                var updateStyleSql = $"UPDATE style SET {string.Join(", ", updateParts)} WHERE style_id = @styleId";
                updateCommand.CommandText = updateStyleSql;
                parameters.Add(_dbHelper.CreateParameter(updateCommand, "@styleId", styleId));
                
                foreach (var param in parameters)
                {
                    updateCommand.Parameters.Add(param);
                }
                
                await updateCommand.ExecuteNonQueryAsync();
            }
        }

        public async Task<object?> GetUpdatedStyleAsync(string styleId)
        {
            var getUpdatedStyleSql = @"
                SELECT style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, location
                FROM style WHERE style_id = @styleId";

            using var getStyleCommand = _context.Database.GetDbConnection().CreateCommand();
            getStyleCommand.CommandText = getUpdatedStyleSql;
            getStyleCommand.Parameters.Add(_dbHelper.CreateParameter(getStyleCommand, "@styleId", styleId));
            
            using var styleReader = await getStyleCommand.ExecuteReaderAsync();
            
            if (await styleReader.ReadAsync())
            {
                return new
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
                    location = styleReader["location"]?.ToString() ?? ""
                };
            }

            return null;
        }
    }
}   