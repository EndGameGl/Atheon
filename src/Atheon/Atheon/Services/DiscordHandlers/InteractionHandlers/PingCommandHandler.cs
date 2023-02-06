using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.EventBus;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using Discord.WebSocket;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

public class PingCommandHandler : SlashCommandHandlerBase
{
    private readonly IEventBus<ClanBroadcastDbModel> _clanBroadcastsChannel;
    private readonly IClanQueue _clanQueue;

    public PingCommandHandler(
        ILogger<PingCommandHandler> logger,
        IEventBus<ClanBroadcastDbModel> clanBroadcastsChannel,
        IClanQueue clanQueue) : base(logger)
    {
        _clanBroadcastsChannel = clanBroadcastsChannel;
        _clanQueue = clanQueue;
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

    [SlashCommand("test-clan-scan", "starts clan scan")]
    public async Task StartClanScan(
        [Summary(description: "Clan id")] long clanId)
    {
        _clanQueue.EnqueueFirstTimeScan(clanId);
        await Context.Interaction.RespondAsync("enqueued clan", ephemeral: true);
    }
}
