using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny;
using Atheon.DataAccess.Models.Destiny.Broadcasts;
using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using Discord.WebSocket;
using DotNetBungieAPI.Service.Abstractions;
using System.Collections.Concurrent;

namespace Atheon.Services.Hosted;

public class BroadcastBackgroundProcessor : PeriodicBackgroundService
{
    private readonly ILogger<BroadcastBackgroundProcessor> _logger;
    private readonly ICommonEvents _commonEvents;
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IDestinyDb _destinyDb;
    private readonly IBroadcastDb _broadcastDb;
    private readonly IGuildDb _guildDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILocalizationService _localizationService;

    private ConcurrentQueue<ClanBroadcastDbModel> _clanBroadcasts = new();
    private ConcurrentQueue<DestinyUserProfileBroadcastDbModel> _userBroadcasts = new();
    private ConcurrentQueue<DestinyUserProfileCustomBroadcastDbModel> _userCustomBroadcasts = new();

    public BroadcastBackgroundProcessor(
        ILogger<BroadcastBackgroundProcessor> logger,
        ICommonEvents commonEvents,
        IDiscordClientProvider discordClientProvider,
        IDestinyDb destinyDb,
        IBroadcastDb broadcastDb,
        IGuildDb guildDb,
        BroadcastSaver broadcastSaver,
        IBungieClientProvider bungieClientProvider,
        EmbedBuilderService embedBuilderService,
        IMemoryCache memoryCache,
        ILocalizationService localizationService) : base(logger)
    {
        _logger = logger;
        _commonEvents = commonEvents;
        _discordClientProvider = discordClientProvider;
        _destinyDb = destinyDb;
        _broadcastDb = broadcastDb;
        _guildDb = guildDb;
        _bungieClientProvider = bungieClientProvider;
        _embedBuilderService = embedBuilderService;
        _memoryCache = memoryCache;
        _localizationService = localizationService;
    }

    protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
    {
        _commonEvents.ClanBroadcasts.Published += (clanBroadcast) => { _clanBroadcasts.Enqueue(clanBroadcast); };
        _commonEvents.ProfileBroadcasts.Published += (userBroadcast) => { _userBroadcasts.Enqueue(userBroadcast); };
        _commonEvents.CustomProfileBroadcasts.Published += (userBroadcast) => { _userCustomBroadcasts.Enqueue(userBroadcast); };

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

        foreach (var userCustomBroadcast in TryDequeue(_userCustomBroadcasts))
        {
            await ProcessUserCustomBroadcast(client, userCustomBroadcast, bungieClient);
        }
    }

    private async Task ProcessUserCustomBroadcast(
        DiscordShardedClient client,
        DestinyUserProfileCustomBroadcastDbModel userBroadcast,
        IBungieClient bungieClient)
    {
        var userName = await _destinyDb.GetProfileDisplayNameAsync(userBroadcast.MembershipId);

        if (string.IsNullOrEmpty(userName))
        {
            await _broadcastDb.MarkUserCustomBroadcastSentAsync(userBroadcast);
            _logger.LogWarning("Failed to send broadcast due to username being null {@Broadcast}", userBroadcast);
            return;
        }

        var clanData = await _destinyDb.GetClanModelAsync(userBroadcast.ClanId);
        if (clanData is null)
        {
            await _broadcastDb.MarkUserCustomBroadcastSentAsync(userBroadcast);
            _logger.LogWarning("Failed to send broadcast due to clan data not being found {@Broadcast}", userBroadcast);
            return;
        }

        var guild = client.GetGuild(userBroadcast.GuildId);
        if (guild is null)
        {
            await _broadcastDb.MarkUserCustomBroadcastSentAsync(userBroadcast);
            _logger.LogWarning("Failed to send broadcast due to guild being null {@Broadcast}", userBroadcast);
            return;
        }

        var settings = await _guildDb.GetGuildSettingsAsync(userBroadcast.GuildId);

        if (settings is null)
            return;

        var channelId = settings.DefaultReportChannel;

        if (channelId is null)
        {
            await _broadcastDb.MarkUserCustomBroadcastSentAsync(userBroadcast);
            _logger.LogWarning("Failed to send broadcast due to channelId being null {@Broadcast}", userBroadcast);
            return;
        }

        var channel = guild.GetTextChannel(channelId.Value);

        var lang = await _memoryCache.GetOrAddAsync(
                $"guild_lang_{userBroadcast.GuildId}",
                async () => (await _guildDb.GetGuildLanguageAsync(userBroadcast.GuildId)).ConvertToBungieLocale(),
                TimeSpan.FromSeconds(15),
                Caching.CacheExpirationType.Absolute);

        if (channel is not null)
        {
            await channel.SendMessageAsync(
                embed: _embedBuilderService.BuildDestinyCustomUserBroadcast(userBroadcast, clanData, bungieClient, userName, lang));

            await _broadcastDb.MarkUserCustomBroadcastSentAsync(userBroadcast);
        }
        else
        {
            await _broadcastDb.MarkUserCustomBroadcastSentAsync(userBroadcast);
            _logger.LogWarning("Failed to send broadcast due to channel being null {@Broadcast}", userBroadcast);
            return;
        }
    }

    private async Task ProcessClanBrocast(
        DiscordShardedClient client,
        ClanBroadcastDbModel clanBroadcast)
    {
        var settings = await _guildDb.GetGuildSettingsAsync(clanBroadcast.GuildId);

        if (settings is null)
            return;

        if (!settings.ReportClanChanges)
            return;

        var clanModel = await _destinyDb.GetClanModelAsync(clanBroadcast.ClanId);

        if (clanModel is null)
            return;

        var clanEmbed = _embedBuilderService.CreateClanBroadcastEmbed(clanBroadcast, clanModel, settings.DestinyManifestLocale.ConvertToBungieLocale());

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
                await _broadcastDb.MarkUserBroadcastSentAsync(broadcast);
                _logger.LogWarning("Failed to send broadcast due to guild being null {@Broadcast}", broadcast);
            }
            return;
        }

        var channelId = await GetGuildProfileBroadcastChannel(broadcasts.First());

        if (channelId is null)
        {
            foreach (var broadcast in broadcasts)
            {
                await _broadcastDb.MarkUserBroadcastSentAsync(broadcast);
                _logger.LogWarning("Failed to send broadcast due to channelId being {@Broadcast}", broadcast);
            }
            return;
        }

        var channel = guild.GetTextChannel(channelId.Value);

        var lang = await _localizationService.GetGuildLocaleCachedAsync(broadcasts.First().GuildId);

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
                await _broadcastDb.MarkUserBroadcastSentAsync(broadcast);
        }
        else
        {
            foreach (var broadcast in broadcasts)
            {
                await _broadcastDb.MarkUserBroadcastSentAsync(broadcast);
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
            await _broadcastDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to username being null {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var clanData = await _destinyDb.GetClanModelAsync(destinyUserBroadcast.ClanId);
        if (clanData is null)
        {
            await _broadcastDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to clan data not being found {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var guild = client.GetGuild(destinyUserBroadcast.GuildId);
        if (guild is null)
        {
            await _broadcastDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to guild being null {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var channelId = await GetGuildProfileBroadcastChannel(destinyUserBroadcast);

        if (channelId is null)
        {
            await _broadcastDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to channelId being null {@Broadcast}", destinyUserBroadcast);
            return;
        }

        var channel = guild.GetTextChannel(channelId.Value);

        var lang = await _localizationService.GetGuildLocaleCachedAsync(destinyUserBroadcast.GuildId);

        if (channel is not null)
        {
            await channel.SendMessageAsync(
                embed: _embedBuilderService.BuildDestinyUserBroadcast(
                    destinyUserBroadcast,
                    clanData,
                    bungieClient,
                    userName,
                    lang));

            await _broadcastDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
        }
        else
        {
            await _broadcastDb.MarkUserBroadcastSentAsync(destinyUserBroadcast);
            _logger.LogWarning("Failed to send broadcast due to channel being null {@Broadcast}", destinyUserBroadcast);
            return;
        }
    }

    private async Task<ulong?> GetGuildProfileBroadcastChannel(DestinyUserProfileBroadcastDbModel destinyUserBroadcast)
    {
        var settings = await _guildDb.GetGuildSettingsAsync(destinyUserBroadcast.GuildId);

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
