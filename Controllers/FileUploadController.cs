using Microsoft.AspNetCore.Mvc;
using Mecha.Helpers;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly SqlConnectionHelper _sqlHelper;
        private readonly long _maxImageAudioFileSize = 10 * 1024 * 1024; // 10MB cho image và audio
        private readonly long _maxVideoFileSize = 50 * 1024 * 1024; // 50MB cho video

        public FileUploadController(IWebHostEnvironment environment, SqlConnectionHelper sqlHelper)
        {
            _environment = environment;
            _sqlHelper = sqlHelper;
        }
        
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string type = "image", [FromQuery] int? userId = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                // Kiểm tra premium status nếu upload background video
                if (type.ToLower() == "background_video")
                {
                    if (!userId.HasValue)
                        return BadRequest(new { message = "User ID is required for video upload" });
                    
                    Console.WriteLine($"Checking premium for user: {userId.Value}, type: {type}");

                    var isPremium = await CheckUserPremiumStatusAsync(userId.Value);
                    Console.WriteLine($"Premium check result: {isPremium}");
                    if (!isPremium)
                        return StatusCode(403, new { message = "Premium subscription required for video background upload" });
                }

                // Kiểm tra kích thước file dựa trên loại
                var maxSize = GetMaxFileSize(type);
                if (file.Length > maxSize)
                    return BadRequest(new { message = $"File size exceeds {maxSize / (1024 * 1024)}MB limit for {type}" });

                var allowedExtensions = GetAllowedExtensions(type);
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = $"File type {fileExtension} is not allowed for {type}" });

                // Tạo thư mục upload theo loại file
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", type);
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"/uploads/{type}/{fileName}";

                // Trả về property "url" để frontend dùng trực tiếp
                return Ok(new
                {
                    message = "File uploaded successfully",
                    url = relativePath,
                    fileName = fileName,
                    originalName = file.FileName,
                    fileSize = file.Length,
                    contentType = file.ContentType,
                    type = type
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromBody] DeleteFileRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Path))
                    return BadRequest(new { message = "File path is required" });

                if (!request.Path.StartsWith("/uploads/"))
                    return BadRequest(new { message = "Invalid file path" });

                var fullPath = Path.Combine(_environment.ContentRootPath, request.Path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
                if (!System.IO.File.Exists(fullPath))
                    return NotFound(new { message = "File not found" });

                System.IO.File.Delete(fullPath);

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting file", error = ex.Message });
            }
        }

        [HttpGet("check-premium/{userId}")]
        public async Task<IActionResult> CheckPremiumStatus(int userId)
        {
            try
            {
                var isPremium = await CheckUserPremiumStatusAsync(userId);
                return Ok(new { isPremium = isPremium });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking premium status", error = ex.Message });
            }
        }

        public class DeleteFileRequest
        {
            public string Path { get; set; } = "";
        }

        private async Task<bool> CheckUserPremiumStatusAsync(int userId)
        {
            try
            {
                var sql = "SELECT Premium FROM users WHERE IdUser = @userId";
                var result = await _sqlHelper.ExecuteScalarAsync(sql,
                    _sqlHelper.CreateParameter("@userId", userId));

                if (result == null || result == DBNull.Value)
                {
                    Console.WriteLine($"User with ID {userId} not found");
                    return false;
                }

                bool isPremium = Convert.ToBoolean(result);
                Console.WriteLine($"User ID: {userId}, Premium value: {isPremium}");
                return isPremium;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking premium status: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private long GetMaxFileSize(string type)
        {
            return type.ToLower() switch
            {
                "background_video" => _maxVideoFileSize, // 50MB cho background video
                "background_image" => _maxImageAudioFileSize, // 10MB cho background image
                "audio" => _maxImageAudioFileSize, // 10MB cho audio
                "audio_image" => _maxImageAudioFileSize, // 10MB cho audio image
                "image" => _maxImageAudioFileSize, // 10MB cho image thông thường
                _ => _maxImageAudioFileSize // Mặc định 10MB
            };
        }

        private string[] GetAllowedExtensions(string type)
        {
            return type.ToLower() switch
            {
                "image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" },
                "audio_image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" },
                "background_image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" },
                "background_video" => new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" },
                "audio" => new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" },
                _ => new[] { ".jpg", ".jpeg", ".png", ".gif" } // Mặc định cho image
            };
        }
    }
    
}