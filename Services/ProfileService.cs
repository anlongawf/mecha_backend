using Microsoft.EntityFrameworkCore;
using Mecha.Data;
using Mecha.DTO;
using Mecha.Helpers;

namespace Mecha.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly DatabaseHelper _dbHelper;
        private readonly StyleService _styleService;

        public ProfileService(AppDbContext context, DatabaseHelper dbHelper, StyleService styleService)
        {
            _context = context;
            _dbHelper = dbHelper;
            _styleService = styleService;
        }

        public async Task<ServiceResult<object>> GetProfileByUsernameAsync(string username)
        {
            try
            {
                var user = _context.Users
                    .Include(u => u.Style)
                    .FirstOrDefault(u => u.Username == username);

                if (user == null)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };

                return new ServiceResult<object>
                {
                    IsSuccess = true,
                    Data = BuildProfileResponse(user),
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
                var user = await _context.Users
                    .Include(u => u.Style)
                    .FirstOrDefaultAsync(u => u.IdUser == id);

                if (user == null)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };

                return new ServiceResult<object>
                {
                    IsSuccess = true,
                    Data = BuildProfileResponse(user),
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
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        StatusCode = 404
                    };

                if (await _context.Users.AnyAsync(u => u.Username == newUsername))
                    return new ServiceResult<object>
                    {
                        IsSuccess = false,
                        Message = "Username already exists",
                        StatusCode = 409
                    };

                user.Username = newUsername;
                await _context.SaveChangesAsync();

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
            finally
            {
                await _dbHelper.EnsureConnectionClosedAsync();
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

        private object BuildProfileResponse(dynamic user)
        {
            if (user.Style == null)
                return new
                {
                    userId = user.IdUser,
                    username = user.Username,
                    email = user.Email,
                    style = (object?)null
                };

            return new
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
                location = user.Style.Location
            };
        }
    }
}