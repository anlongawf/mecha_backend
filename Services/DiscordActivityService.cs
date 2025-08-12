using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace Mecha.Services
{
    public class DiscordActivityService
    {
        private readonly DiscordSocketClient _client;
        private readonly ConcurrentDictionary<ulong, SocketPresence> _userPresence 
            = new ConcurrentDictionary<ulong, SocketPresence>();

        public DiscordActivityService()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
            });

            _client.PresenceUpdated += OnPresenceUpdated;
        }

        public async Task StartAsync(string token)
        {
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        private Task OnPresenceUpdated(SocketUser user, SocketPresence oldPresence, SocketPresence newPresence)
        {
            _userPresence[user.Id] = newPresence;
            return Task.CompletedTask;
        }

        public object? GetActivity(ulong discordId)
        {
            if (_userPresence.TryGetValue(discordId, out var presence))
            {
                return new
                {
                    Status = presence.Status.ToString(),
                    Activities = presence.Activities.Select(a => new
                    {
                        a.Name,
                        a.Type,
                        a.Details
                    }).ToList()
                };
            }
            return null;
        }
    }
}