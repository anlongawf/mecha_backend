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

        if (file.Length > _maxFileSize)
            return BadRequest(new { message = $"File size exceeds {_maxFileSize / (1024 * 1024)}MB limit" });

        var allowedExtensions = GetAllowedExtensions(type);
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { message = $"File type {fileExtension} is not allowed for {type}" });

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
            contentType = file.ContentType
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
    }
}

[HttpDelete("delete")]
public IActionResult DeleteFile([FromBody] DeleteFileRequest request)
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

        public class DeleteFileRequest
        {
            public string Path { get; set; } = "";
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