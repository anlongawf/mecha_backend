using Microsoft.AspNetCore.Mvc;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

        public FileUploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string type = "image")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                // Kiểm tra kích thước file
                if (file.Length > _maxFileSize)
                    return BadRequest(new { message = $"File size exceeds {_maxFileSize / (1024 * 1024)}MB limit" });

                // Kiểm tra loại file
                var allowedExtensions = GetAllowedExtensions(type);
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = $"File type {fileExtension} is not allowed for {type}" });

                // Tạo thư mục uploads nếu chưa có
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", type);
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Trả về đường dẫn relative
                var relativePath = $"/uploads/{type}/{fileName}";

                return Ok(new
                {
                    message = "File uploaded successfully",
                    filePath = relativePath,
                    fileName = fileName,
                    originalName = file.FileName,
                    fileSize = file.Length,
                    contentType = file.ContentType
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public IActionResult DeleteFile([FromQuery] string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return BadRequest(new { message = "File path is required" });

                // Chỉ cho phép xóa file trong thư mục uploads
                if (!filePath.StartsWith("/uploads/"))
                    return BadRequest(new { message = "Invalid file path" });

                var fullPath = Path.Combine(_environment.ContentRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
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

        private string[] GetAllowedExtensions(string type)
        {
            return type.ToLower() switch
            {
                "image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" },
                "audio_image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" },
                "audio" => new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" },
                _ => new[] { ".jpg", ".jpeg", ".png", ".gif" }
            };
        }

    }
}