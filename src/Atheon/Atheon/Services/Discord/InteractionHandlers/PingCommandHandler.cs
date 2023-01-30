using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.Discord.InteractionHandlers.Base;
using Atheon.Services.EventBus;
using Discord.Interactions;
using Discord.WebSocket;

namespace Atheon.Services.Discord.InteractionHandlers;

public class PingCommandHandler : SlashCommandHandlerBase
{
    private readonly IEventBus<ClanBroadcastDbModel> _clanBroadcastsChannel;

    public PingCommandHandler(
        ILogger<PingCommandHandler> logger,
        IEventBus<ClanBroadcastDbModel> clanBroadcastsChannel) : base(logger)
    {
        _clanBroadcastsChannel = clanBroadcastsChannel;
    }

    [SlashCommand("ping", "pongs back")]
    public async Task PingPongAsync()
    {
        await Context.Interaction.RespondAsync("pong");
    }

    [SlashCommand("test-clan-broadcast", "sends test broadcast")]
    public async Task TestClanBroadcastAsync()
    {
        _clanBroadcastsChannel.Publish(new ClanBroadcastDbModel()
        {
            ClanId = 4394229,
            Date = DateTime.UtcNow,
            GuildId = 886500502060302357,
            Type = ClanBroadcastType.ClanScanFinished,
            WasAnnounced = false
        });
        await Context.Interaction.RespondAsync("hi", ephemeral: true);
    }
}
