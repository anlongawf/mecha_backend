using Microsoft.Extensions.Hosting;

namespace Mecha.Services
{
    public class DiscordBotHostedService : IHostedService
    {
        private readonly DiscordActivityService _activityService;
        private readonly IConfiguration _config;

        public DiscordBotHostedService(DiscordActivityService activityService, IConfiguration config)
        {
            _activityService = activityService;
            _config = config;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var token = _config["Discord:BotToken"];
            await _activityService.StartAsync(token);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}