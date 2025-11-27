using Mecha.DTO;
using Mecha.Helpers;
using MySql.Data.MySqlClient;

namespace Mecha.Services
{
    public class StyleService
    {
        private readonly SqlConnectionHelper _sqlHelper;
        private readonly DatabaseHelper _dbHelper;

        public StyleService(SqlConnectionHelper sqlHelper, DatabaseHelper dbHelper)
        {
            _sqlHelper = sqlHelper;
            _dbHelper = dbHelper;
        }

        public async Task<string> CreateNewStyleAsync(UpdateProfileDto dto, string username)
        {
            var styleId = Guid.NewGuid().ToString();
            
            var insertStyleSql = @"
                INSERT INTO style (style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, location)
                VALUES (@styleId, @profileAvatar, @background, @audio, @audioImage, @audioTitle, @customCursor, @description, @username, @location)";
                                
            await _sqlHelper.ExecuteNonQueryAsync(insertStyleSql,
                _sqlHelper.CreateParameter("@styleId", styleId),
                _sqlHelper.CreateParameter("@profileAvatar", dto.ProfileAvatar),
                _sqlHelper.CreateParameter("@background", dto.Background),
                _sqlHelper.CreateParameter("@audio", dto.Audio),
                _sqlHelper.CreateParameter("@audioImage", dto.AudioImage),
                _sqlHelper.CreateParameter("@audioTitle", dto.AudioTitle),
                _sqlHelper.CreateParameter("@customCursor", dto.CustomCursor),
                _sqlHelper.CreateParameter("@description", dto.Description),
                _sqlHelper.CreateParameter("@username", username),
                _sqlHelper.CreateParameter("@location", dto.Location));
            
            return styleId;
        }

        public async Task UpdateUserStyleIdAsync(int userId, string styleId)
        {
            var updateUserSql = "UPDATE users SET StyleId = @styleId WHERE IdUser = @userId";
            await _sqlHelper.ExecuteNonQueryAsync(updateUserSql,
                _sqlHelper.CreateParameter("@styleId", styleId),
                _sqlHelper.CreateParameter("@userId", userId));
        }

        public async Task UpdateExistingStyleAsync(string styleId, UpdateProfileDto dto, string? updatedUsername = null, string? currentUsername = null)
        {
            var updateParts = new List<string>();
            var parameters = new List<MySqlParameter>();
            
            if (dto.ProfileAvatar != null)
            {
                updateParts.Add("profile_avatar = @profileAvatar");
                parameters.Add(_sqlHelper.CreateParameter("@profileAvatar", dto.ProfileAvatar));
            }
            if (dto.Background != null)
            {
                updateParts.Add("background = @background");
                parameters.Add(_sqlHelper.CreateParameter("@background", dto.Background));
            }
            if (dto.Audio != null)
            {
                updateParts.Add("audio = @audio");
                parameters.Add(_sqlHelper.CreateParameter("@audio", dto.Audio));
            }
            if (dto.CustomCursor != null)
            {
                updateParts.Add("custom_cursor = @customCursor");
                parameters.Add(_sqlHelper.CreateParameter("@customCursor", dto.CustomCursor));
            }
            if (dto.Description != null)
            {
                updateParts.Add("description = @description");
                parameters.Add(_sqlHelper.CreateParameter("@description", dto.Description));
            }
            if (dto.Location != null)
            {
                updateParts.Add("location = @location");
                parameters.Add(_sqlHelper.CreateParameter("@location", dto.Location));
            }
            if (dto.AudioImage != null)
            {
                updateParts.Add("AudioImage = @audioImage");
                parameters.Add(_sqlHelper.CreateParameter("@audioImage", dto.AudioImage));
            }
            if (dto.AudioTitle != null)
            {
                updateParts.Add("AudioTitle = @audioTitle");
                parameters.Add(_sqlHelper.CreateParameter("@audioTitle", dto.AudioTitle));
            }

            // Update username in style if changed
            if (!string.IsNullOrEmpty(updatedUsername) && updatedUsername != currentUsername)
            {
                updateParts.Add("username = @username");
                parameters.Add(_sqlHelper.CreateParameter("@username", updatedUsername));
            }
            else if (dto.Username != null && string.IsNullOrEmpty(updatedUsername))
            {
                updateParts.Add("username = @username");
                parameters.Add(_sqlHelper.CreateParameter("@username", dto.Username));
            }

            if (updateParts.Any())
            {
                var updateStyleSql = $"UPDATE style SET {string.Join(", ", updateParts)} WHERE style_id = @styleId";
                parameters.Add(_sqlHelper.CreateParameter("@styleId", styleId));
                
                await _sqlHelper.ExecuteNonQueryAsync(updateStyleSql, parameters.ToArray());
            }
        }

        public async Task<object?> GetUpdatedStyleAsync(string styleId)
        {
            var getUpdatedStyleSql = @"
                SELECT style_id, profile_avatar, background, audio, AudioImage, AudioTitle, custom_cursor, description, username, location
                FROM style WHERE style_id = @styleId";

            using var reader = await _sqlHelper.ExecuteReaderAsync(getUpdatedStyleSql,
                _sqlHelper.CreateParameter("@styleId", styleId));
            
            if (await reader.ReadAsync())
            {
                return new
                {
                    styleId = reader["style_id"] == DBNull.Value ? "" : reader["style_id"]?.ToString(),
                    profileAvatar = reader["profile_avatar"] == DBNull.Value ? "" : reader["profile_avatar"]?.ToString(),
                    background = reader["background"] == DBNull.Value ? "" : reader["background"]?.ToString(),
                    audio = reader["audio"] == DBNull.Value ? "" : reader["audio"]?.ToString(),
                    audioImage = reader["AudioImage"] == DBNull.Value ? "" : reader["AudioImage"]?.ToString(),
                    audioTitle = reader["AudioTitle"] == DBNull.Value ? "" : reader["AudioTitle"]?.ToString(),
                    customCursor = reader["custom_cursor"] == DBNull.Value ? "" : reader["custom_cursor"]?.ToString(),
                    description = reader["description"] == DBNull.Value ? "" : reader["description"]?.ToString(),
                    username = reader["username"] == DBNull.Value ? "" : reader["username"]?.ToString(),
                    location = reader["location"] == DBNull.Value ? "" : reader["location"]?.ToString()
                };
            }

            return null;
        }
    }
}   