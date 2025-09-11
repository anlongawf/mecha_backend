using Mecha.DTO;

namespace Mecha.Services
{
    public interface IProfileService
    {
        Task<ServiceResult<object>> GetProfileByUsernameAsync(string username);
        Task<ServiceResult<object>> GetProfileByIdAsync(int id);
        Task<ServiceResult<object>> UpdateProfileAsync(int id, UpdateProfileDto dto);
        Task<ServiceResult<object>> UpdateProfileByUsernameAsync(string username, UpdateProfileDto dto);
        Task<ServiceResult<object>> ChangeUsernameAsync(int id, string newUsername);
    }

    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 200;
    }
}