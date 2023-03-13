using Atheon.Extensions;
using Atheon.Models.Database.Destiny;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers;
using Atheon.Services.EventBus;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using Discord.WebSocket;
using DotNetBungieAPI.Service.Abstractions;
using System.Collections.Concurrent;

namespace Atheon.Services.Hosted;

public class BroadcastBackgroundProcessor : PeriodicBackgroundService
{
    private readonly ILogger<BroadcastBackgroundProcessor> _logger;
    private readonly IEventBus<ClanBroadcastDbModel> _clanBroadcastEventChannel;
    private readonly IEventBus<DestinyUserProfileBroadcastDbModel> _profileBroadcastEventChannel;
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IMemoryCache _memoryCache;
    private ConcurrentQueue<ClanBroadcastDbModel> _clanBroadcasts = new();
    private ConcurrentQueue<DestinyUserProfileBroadcastDbModel> _userBroadcasts = new();

    public BroadcastBackgroundProcessor(
        ILogger<BroadcastBackgroundProcessor> logger,
        IEventBus<ClanBroadcastDbModel> clanBroadcastEventChannel,
        IEventBus<DestinyUserProfileBroadcastDbModel> profileBroadcastEventChannel,
        IDiscordClientProvider discordClientProvider,
        IDestinyDb destinyDb,
        BroadcastSaver broadcastSaver,
        IBungieClientProvider bungieClientProvider,
        EmbedBuilderService embedBuilderService,
        DestinyDefinitionDataService destinyDefinitionDataService,
        IMemoryCache memoryCache) : base(logger)
    {
        _logger = logger;
        _clanBroadcastEventChannel = clanBroadcastEventChannel;
        _profileBroadcastEventChannel = profileBroadcastEventChannel;
        _discordClientProvider = discordClientProvider;
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _embedBuilderService = embedBuilderService;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _memoryCache = memoryCache;
    }

    protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
    {
        _clanBroadcastEventChannel.Published += (clanBroadcast) => { _clanBroadcasts.Enqueue(clanBroadcast); };
        _profileBroadcastEventChannel.Published += (userBroadcast) => { _userBroadcasts.Enqueue(userBroadcast); };

        this.ChangeTimerSafe(TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    protected override async Task OnTimerExecuted(CancellationToken cancellationToken)
    {
        if (!_discordClientProvider.IsReady)
            return;

        var client = _discordClientProvider.Client!;
        var bungieClient = await _bungieClientProvider.GetClientAsync();

        foreach (var clanBroadcast in TryDequeue(_clanBroadcasts))
        {
            await ProcessClanBrocast(client, clanBroadcast);
        }

        var userBroadcasts = TryDequeue(_userBroadcasts).ToList();

        foreach (var guildGroupedBroadcasts in userBroadcasts.GroupBy(x => x.GuildId))
        {
            var chunkedGuilds = guildGroupedBroadcasts.Chunk(24);
            foreach (var guildsChunk in chunkedGuilds)
                foreach (var typeGroupedBroadcasts in guildsChunk.GroupBy(x => x.Type))
                    foreach (var hashGroupedBroadcasts in typeGroupedBroadcasts.GroupBy(x => x.DefinitionHash))
                    {
                        await SendGroupedUserBroadcasts(
                            client,
                            bungieClient,
                            guildGroupedBroadcasts.Key,
                            typeGroupedBroadcasts.Key,
                            hashGroupedBroadcasts.Key,
                            hashGroupedBroadcasts,
                            cancellationToken);
                    }
        }
    }

    private async Task ProcessClanBrocast(
        DiscordShardedClient client,
        ClanBroadcastDbModel clanBroadcast)
    {
        var settings = await _destinyDb.GetGuildSettingsAsync(clanBroadcast.GuildId);

        if (settings is null)
            return;

        if (!settings.ReportClanChanges)
            return;

        var clanModel = await _destinyDb.GetClanModelAsync(clanBroadcast.ClanId);

        if (clanModel is null)
            return;

        var clanEmbed = _embedBuilderService.CreateClanBroadcastEmbed(clanBroadcast, clanModel);

        var guild = client.GetGuild(clanBroadcast.GuildId);
        var channel = guild.GetTextChannel(settings.DefaultReportChannel.Value);

        await channel.SendMessageAsync(embed: clanEmbed);
    }

    private async Task SendGroupedUserBroadcasts(
        DiscordShardedClient client,
        IBungieClient bungieClient,
        ulong guildId,
        ProfileBroadcastType broadcastType,
        uint definitionHash,
        IGrouping<uint, DestinyUserProfileBroadcastDbModel> broadcasts,
        CancellationToken cancellationToken)
    {
        var amountOfBroadcasts = broadcasts.Count();
        if (amountOfBroadcasts == 1)
        {
            await SendUserBroadcast(broadcasts.First(), client, bungieClient);
            return;
        }

        var userNamesKeyedByMembershipId = new Dictionary<long, string>(amountOfBroadcasts);
        foreach (var broadcast in broadcasts)
        {
            var userName = await _destinyDb.GetProfileDisplayNameAsync(broadcast.MembershipId);

            if (string.IsNullOrEmpty(userName))
                continue;

            userNamesKeyedByMembershipId.Add(broadcast.MembershipId, userName);
        }

        var clanIds = broadcasts
            .DistinctBy(x => x.ClanId)
            .Select(x => x.ClanId);

        var clansData = new List<DestinyClanDbModel>();

        foreach (var clanId in clanIds)
        {
            var clanData = await _destinyDb.GetClanModelAsync(clanId);
            if (clanData is null)
                return;

            clansData.Add(clanData);
        }

        var clansDictionary = clansData.ToDictionary(x => x.ClanId, x => x);

        var guild = client.GetGuild(guildId);
        if (guild is null)
        {

            foreach (var broadcast in broadcasts)
            {
                await _destinyDb.MarkUserBroadcastSentAsync(broadcast);
                _logger.LogWarning("Failed to send broadcast due to guild being null {@Broadcast}", broadcast);
            }
            return;
        }

        var channelId = await GetGuildProfileBroadcastChannel(broadcasts.First());

        if (channelId is null)
        {
            foreach (var broadcast in broadcasts)
            {
                await _destinyDb.MarkUserBroadcastSentAsync(broadcast);
                _logger.LogWarning("Failed to send broadcast due to channelId being {@Broadcast}", broadcast);
            }
            return;
        }

        var channel = guild.GetTextChannel(channelId.Value);

        var lang = await _memoryCache.GetOrAddAsync(
                $"guild_lang_{broadcasts.First().GuildId}",
                async () => (await _destinyDb.GetGuildLanguageAsync(broadcasts.First().GuildId)).ConvertToBungieLocale(),
                TimeSpan.FromSeconds(15),
                Caching.CacheExpirationType.Absolute);

        if (channel is not null)
        {
            var embed = _embedBuilderService.BuildDestinyUserGroupedBroadcast(
                broadcasts,
                broadcastType,
                definitionHash,
                clansDictionary,
                bungieClient,
                userNamesKeyedByMembershipId,
                lang);

            await channel.SendMessageAsync(embed: embed);

            foreach (var broadcast in broadcasts)
                await _destinyDb.MarkUserBroadcastSentAsync(broadcast);
        }
        else
        {
            foreach (var broadcast in broadcasts)
            {
                await _destinyDb.MarkUserBroadcastSentAsync(broadcast);
                _logger.LogWarning("Failed to send broadcast due to channel being null {@Broadcast}", broadcast);
            }
            return;
        }
    }

    private async Task SendUserBroadcast(
        DestinyUserProfileBroadcastDbModel destinyUserBroadcast,
        DiscordShardedClient client,
        IBungieClient bungieClient)
    {
        var userName = await _destinyDb.GetProfileDisplayNameAsync(destinyUserBroadcast.MembershipId);

        if (string.IsNullOrEmpty(userName))
        {
            await _destinyDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to username being null {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var clanData = await _destinyDb.GetClanModelAsync(destinyUserBroadcast.ClanId);
        if (clanData is null)
        {
            await _destinyDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to clan data not being found {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var guild = client.GetGuild(destinyUserBroadcast.GuildId);
        if (guild is null)
        {
            await _destinyDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to guild being null {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var channelId = await GetGuildProfileBroadcastChannel(destinyUserBroadcast);

        if (channelId is null)
        {
            await _destinyDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to channelId being null {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var channel = guild.GetTextChannel(channelId.Value);

        var lang = await _memoryCache.GetOrAddAsync(
                $"guild_lang_{destinyUserBroadcast.GuildId}",
                async () => (await _destinyDb.GetGuildLanguageAsync(destinyUserBroadcast.GuildId)).ConvertToBungieLocale(),
                TimeSpan.FromSeconds(15),
                Caching.CacheExpirationType.Absolute);

        if (channel is not null)
        {
            await channel.SendMessageAsync(
                embed: _embedBuilderService.BuildDestinyUserBroadcast(
                    destinyUserBroadcast,
                    clanData,
                    bungieClient,
                    userName,
                    lang));

            await _destinyDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
        }
        else
        {
            await _destinyDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to channel being null {@Broadcast}", destinyUserBroadcast);
            return;
        }
    }

    private async Task<ulong?> GetGuildProfileBroadcastChannel(DestinyUserProfileBroadcastDbModel destinyUserBroadcast)
    {
        var settings = await _destinyDb.GetGuildSettingsAsync(destinyUserBroadcast.GuildId);

        if (settings is null)
            return null;

        if (destinyUserBroadcast.Type is ProfileBroadcastType.Title or ProfileBroadcastType.GildedTitle or ProfileBroadcastType.Triumph &&
            settings.TrackedRecords.OverrideReportChannel is not null)
        {
            return settings.TrackedRecords.OverrideReportChannel;
        }

        if (destinyUserBroadcast.Type is ProfileBroadcastType.Collectible &&
            settings.TrackedCollectibles.OverrideReportChannel is not null)
        {
            return settings.TrackedCollectibles.OverrideReportChannel;
        }

        return settings.DefaultReportChannel;
    }

    private IEnumerable<T> TryDequeue<T>(ConcurrentQueue<T> source)
    {
        while (source.TryDequeue(out var item))
        {
            yield return item;
        }
    }
}
