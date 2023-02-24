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

    [SlashCommand("item-check", "Checks who has items")]
    public async Task GetUsersWithItem(
        [Autocomplete(typeof(DestinyCollectibleDefinitionAutocompleter))] [Summary(description: "Collectible")] string collectibleHash)
    {
        var itemHash = uint.Parse(collectibleHash);
        var users = await _destinyDb.GetProfilesWithCollectibleAsync(itemHash);

        var bungieClient = await _bungieClientProvider.GetClientAsync();

        bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, DotNetBungieAPI.Models.BungieLocales.EN, out var colDef);

        await Context.Interaction.RespondAsync(
            embed: EmbedBuilders.Embeds.GetGenericEmbed(
                $"Users who have {colDef.DisplayProperties.Name}",
                Color.Red,
                description: "> " + string.Join("\n> ", users.Select(x => x.Name))).Build(),
            ephemeral: true);
    }
}
