using Discord.WebSocket;

namespace Atheon.Services.Interfaces
{
    public interface IDiscordClientProvider
    {
        bool IsReady { get; }
        DiscordShardedClient? Client { get; }
        Task ConnectAsync();
        Task ForceReloadClientAsync();
    }
}
