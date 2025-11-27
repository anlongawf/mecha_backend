using Mecha.DTO;
using Mecha.Helpers;
using MySql.Data.MySqlClient;

namespace Mecha.Services
{
    public class ProfileService : IProfileService
    {
        private readonly SqlConnectionHelper _sqlHelper;
        private readonly DatabaseHelper _dbHelper;
        private readonly StyleService _styleService;

        public ProfileService(SqlConnectionHelper sqlHelper, DatabaseHelper dbHelper, StyleService styleService)
        {
            _sqlHelper = sqlHelper;
            _dbHelper = dbHelper;
            _styleService = styleService;
        }

        public async Task<ServiceResult<object>> GetProfileByUsernameAsync(string username)
        {
            try
            {
                var sql = @"
                    SELECT u.IdUser, u.Username, u.Email, u.StyleId,
                           s.style_id, s.profile_avatar, s.background, s.audio, s.AudioImage, s.AudioTitle,
                           s.custom_cursor, s.description, s.username as style_username, s.location
                    FROM users u
                    LEFT JOIN style s ON u.StyleId = s.style_id
                    WHERE u.Username = @username";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@username", username));

                if (!await reader.ReadAsync())
                {
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };
                }

                var userId = Convert.ToInt32(reader["IdUser"]);
                var userUsername = reader["Username"] == DBNull.Value ? null : reader["Username"]?.ToString();
                var email = reader["Email"] == DBNull.Value ? null : reader["Email"]?.ToString();
                var styleId = reader["StyleId"] == DBNull.Value ? null : reader["StyleId"]?.ToString();

                if (styleId == null || reader["style_id"] == DBNull.Value)
                {
                    return new ServiceResult<object>
                    {
                        IsSuccess = true,
                        Data = new
                        {
                            userId = userId,
                            username = userUsername,
                            email = email,
                            style = (object?)null
                        },
                        StatusCode = 200
                    };
                }

                return new ServiceResult<object>
                {
                    IsSuccess = true,
                    Data = new
                    {
                        userId = userId,
                        styleId = reader["style_id"]?.ToString(),
                        profileAvatar = reader["profile_avatar"] == DBNull.Value ? null : reader["profile_avatar"]?.ToString(),
                        background = reader["background"] == DBNull.Value ? null : reader["background"]?.ToString(),
                        audio = reader["audio"] == DBNull.Value ? null : reader["audio"]?.ToString(),
                        audioImage = reader["AudioImage"] == DBNull.Value ? null : reader["AudioImage"]?.ToString(),
                        audioTitle = reader["AudioTitle"] == DBNull.Value ? null : reader["AudioTitle"]?.ToString(),
                        customCursor = reader["custom_cursor"] == DBNull.Value ? null : reader["custom_cursor"]?.ToString(),
                        description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString(),
                        username = reader["style_username"] == DBNull.Value ? null : reader["style_username"]?.ToString(),
                        location = reader["location"] == DBNull.Value ? null : reader["location"]?.ToString()
                    },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ServiceResult<object>> GetProfileByIdAsync(int id)
        {
            try
            {
                var sql = @"
                    SELECT u.IdUser, u.Username, u.Email, u.StyleId,
                           s.style_id, s.profile_avatar, s.background, s.audio, s.AudioImage, s.AudioTitle,
                           s.custom_cursor, s.description, s.username as style_username, s.location
                    FROM users u
                    LEFT JOIN style s ON u.StyleId = s.style_id
                    WHERE u.IdUser = @id";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@id", id));

                if (!await reader.ReadAsync())
                {
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };
                }

                var userId = Convert.ToInt32(reader["IdUser"]);
                var userUsername = reader["Username"] == DBNull.Value ? null : reader["Username"]?.ToString();
                var email = reader["Email"] == DBNull.Value ? null : reader["Email"]?.ToString();
                var styleId = reader["StyleId"] == DBNull.Value ? null : reader["StyleId"]?.ToString();

                if (styleId == null || reader["style_id"] == DBNull.Value)
                {
                    return new ServiceResult<object>
                    {
                        IsSuccess = true,
                        Data = new
                        {
                            userId = userId,
                            username = userUsername,
                            email = email,
                            style = (object?)null
                        },
                        StatusCode = 200
                    };
                }

                return new ServiceResult<object>
                {
                    IsSuccess = true,
                    Data = new
                    {
                        userId = userId,
                        styleId = reader["style_id"]?.ToString(),
                        profileAvatar = reader["profile_avatar"] == DBNull.Value ? null : reader["profile_avatar"]?.ToString(),
                        background = reader["background"] == DBNull.Value ? null : reader["background"]?.ToString(),
                        audio = reader["audio"] == DBNull.Value ? null : reader["audio"]?.ToString(),
                        audioImage = reader["AudioImage"] == DBNull.Value ? null : reader["AudioImage"]?.ToString(),
                        audioTitle = reader["AudioTitle"] == DBNull.Value ? null : reader["AudioTitle"]?.ToString(),
                        customCursor = reader["custom_cursor"] == DBNull.Value ? null : reader["custom_cursor"]?.ToString(),
                        description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString(),
                        username = reader["style_username"] == DBNull.Value ? null : reader["style_username"]?.ToString(),
                        location = reader["location"] == DBNull.Value ? null : reader["location"]?.ToString()
                    },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ServiceResult<object>> ChangeUsernameAsync(int id, string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Username cannot be empty",
                    StatusCode = 400
                };

            try
            {
                // Check if user exists
                var checkUserSql = "SELECT COUNT(*) FROM users WHERE IdUser = @id";
                var userExists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkUserSql,
                    _sqlHelper.CreateParameter("@id", id))) > 0;

                if (!userExists)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };

                if (await _dbHelper.IsUsernameExistsAsync(newUsername, id))
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "Username already exists",
                        StatusCode = 409
                    };

                await _dbHelper.UpdateUsernameAsync(id, newUsername);

                return new ServiceResult<object>
                {
                    IsSuccess = true,
                    Data = new { message = "Username updated successfully", username = newUsername },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ServiceResult<object>> UpdateProfileAsync(int id, UpdateProfileDto dto)
        {
            if (dto == null)
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid body",
                    StatusCode = 400
                };

            try
            {
                var (userId, currentUsername, currentStyleId) = await _dbHelper.GetUserInfoByIdAsync(id);
                
                if (userId == null || currentUsername == null)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };

                // Handle username change
                string updatedUsername = currentUsername;
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != currentUsername)
                {
                    if (await _dbHelper.IsUsernameExistsAsync(dto.Username, id))
                        return new ServiceResult<object>
                        {
                            IsSuccess = false,
                            Message = "Username already exists",
                            StatusCode = 409
                        };

                    await _dbHelper.UpdateUsernameAsync(id, dto.Username);
                    updatedUsername = dto.Username;
                }

                string styleId;

                if (string.IsNullOrEmpty(currentStyleId))
                {
                    // Create new style
                    styleId = await _styleService.CreateNewStyleAsync(dto, updatedUsername);
                    await _styleService.UpdateUserStyleIdAsync(id, styleId);
                }
                else
                {
                    // Update existing style
                    styleId = currentStyleId;
                    await _styleService.UpdateExistingStyleAsync(styleId, dto, updatedUsername, currentUsername);
                }

                var updatedStyle = await _styleService.GetUpdatedStyleAsync(styleId);
                
                if (updatedStyle == null)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "Failed to retrieve updated style",
                        StatusCode = 500
                    };

                return new ServiceResult<object>
                {
                    IsSuccess = true,
                    Data = new
                    {
                        message = "Profile updated successfully",
                        Style = updatedStyle,
                        newUsername = updatedUsername != currentUsername ? updatedUsername : null
                    },
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while updating the profile: {ex.Message}",
                    StatusCode = 500
                };
            }
        }

        public async Task<ServiceResult<object>> UpdateProfileByUsernameAsync(string username, UpdateProfileDto dto)
        {
            if (dto == null)
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid body",
                    StatusCode = 400
                };

            if (string.IsNullOrWhiteSpace(username))
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = "Username is required",
                    StatusCode = 400
                };

            try
            {
                var (userId, currentUsername, currentStyleId) = await _dbHelper.GetUserInfoByUsernameAsync(username);
                
                if (userId == null)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };

                // Delegate to UpdateProfileAsync with the found user ID
                return await UpdateProfileAsync(userId.Value, dto);
            }
            catch (Exception ex)
            {
                return new ServiceResult<object>
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}",
                    StatusCode = 500
                };
            }
        }
    }
}