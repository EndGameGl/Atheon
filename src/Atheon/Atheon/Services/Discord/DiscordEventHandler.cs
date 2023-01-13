using Atheon.Models.Destiny;
using Atheon.Services.Interfaces;
using Discord.WebSocket;

namespace Atheon.Services.Discord;

public class DiscordEventHandler : IDiscordEventHandler
{
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IDbAccess _dbAccess;

    public DiscordEventHandler(
        IDiscordClientProvider discordClientProvider,
        IDbAccess dbAccess)
    {
        _discordClientProvider = discordClientProvider;
        _dbAccess = dbAccess;
    }

    public void SubscribeToEvents()
    {
        if (!_discordClientProvider.IsReady)
        {
            return;
        }

        var client = _discordClientProvider.Client!;

        client.JoinedGuild += OnGuildJoin;
        client.LeftGuild += OnGuildLeft;
    }

    private async Task OnGuildJoin(SocketGuild socketGuild)
    {
        var newGuildData = GuildSettings.CreateDefault(socketGuild.Id, socketGuild.Name);

        await _dbAccess.UpsertGuildSettingsAsync(newGuildData);
    }

    private async Task OnGuildLeft(SocketGuild socketGuild)
    {
        await _dbAccess.DeleteGuildSettingsAsync(socketGuild.Id);
    }
}
