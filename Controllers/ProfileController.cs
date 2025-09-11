using Microsoft.AspNetCore.Mvc;
using Mecha.DTO;
using Mecha.Services;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// Change username for a specific user by ID
        /// </summary>
        [HttpPut("{id}/change-username")]
        public async Task<IActionResult> ChangeUsername(int id, [FromBody] string newUsername)
        {
            var result = await _profileService.ChangeUsernameAsync(id, newUsername);
            
            if (result.IsSuccess)
                return Ok(result.Data);
            
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Get profile by username
        /// </summary>
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetProfileByUsername(string username)
        {
            var result = await _profileService.GetProfileByUsernameAsync(username);
            
            if (result.IsSuccess)
                return Ok(result.Data);
            
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Get profile by user ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfileById(int id)
        {
            var result = await _profileService.GetProfileByIdAsync(id);
            
            if (result.IsSuccess)
                return Ok(result.Data);
            
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Update profile by user ID (POST method)
        /// </summary>
        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateProfileByIdPost(int id, [FromBody] UpdateProfileDto dto)
        {
            var result = await _profileService.UpdateProfileAsync(id, dto);
            
            if (result.IsSuccess)
                return Ok(result.Data);
            
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Update profile by user ID (PUT method)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfileByIdPut(int id, [FromBody] UpdateProfileDto dto)
        {
            var result = await _profileService.UpdateProfileAsync(id, dto);
            
            if (result.IsSuccess)
                return Ok(result.Data);
            
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        /// <summary>
        /// Update profile by username (backward compatibility)
        /// </summary>
        [HttpPost("username/{username}")]
        public async Task<IActionResult> UpdateProfileByUsername(string username, [FromBody] UpdateProfileDto dto)
        {
            var result = await _profileService.UpdateProfileByUsernameAsync(username, dto);
            
            if (result.IsSuccess)
                return Ok(result.Data);
            
            return StatusCode(result.StatusCode, new { message = result.Message });
        }
    }
}