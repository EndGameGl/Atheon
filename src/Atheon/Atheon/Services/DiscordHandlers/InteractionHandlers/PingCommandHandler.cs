using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.DiscordHandlers.Autocompleters;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.EventBus;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

public class PingCommandHandler : SlashCommandHandlerBase
{
    private readonly IEventBus<ClanBroadcastDbModel> _clanBroadcastsChannel;
    private readonly IClanQueue _clanQueue;
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;

    public PingCommandHandler(
        ILogger<PingCommandHandler> logger,
        IEventBus<ClanBroadcastDbModel> clanBroadcastsChannel,
        IClanQueue clanQueue,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider) : base(logger)
    {
        _clanBroadcastsChannel = clanBroadcastsChannel;
        _clanQueue = clanQueue;
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
    }

    
}
