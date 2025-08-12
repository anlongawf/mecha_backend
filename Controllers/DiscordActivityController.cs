using Microsoft.AspNetCore.Mvc;
using Mecha.Services;

namespace Mecha.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscordActivityController : ControllerBase
    {
        private readonly DiscordActivityService _activityService;

        public DiscordActivityController(DiscordActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpGet("{discordId}")]
        public IActionResult GetUserActivity(string discordId)
        {
            if (!ulong.TryParse(discordId, out var id))
                return BadRequest("Invalid Discord ID");

            var data = _activityService.GetActivity(id);
            if (data == null)
                return NotFound(new { message = "No activity found" });

            return Ok(data);
        }
    }
}