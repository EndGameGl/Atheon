using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers.EmbedBuilders;
using Atheon.Services.EventBus;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using Discord.WebSocket;
using DotNetBungieAPI.Service.Abstractions;
using System.Collections.Concurrent;

namespace Atheon.Services.Hosted
{
    public class BroadcastBackgroundProcessor : PeriodicBackgroundService
    {
        private readonly ILogger<BroadcastBackgroundProcessor> _logger;
        private readonly IEventBus<ClanBroadcastDbModel> _clanBroadcastEventChannel;
        private readonly IEventBus<DestinyUserProfileBroadcastDbModel> _profileBroadcastEventChannel;
        private readonly IDiscordClientProvider _discordClientProvider;
        private readonly IDestinyDb _destinyDb;
        private readonly IBungieClientProvider _bungieClientProvider;
        private ConcurrentQueue<ClanBroadcastDbModel> _clanBroadcasts = new();
        private ConcurrentQueue<DestinyUserProfileBroadcastDbModel> _userBroadcasts = new();

        public BroadcastBackgroundProcessor(
            ILogger<BroadcastBackgroundProcessor> logger,
            IEventBus<ClanBroadcastDbModel> clanBroadcastEventChannel,
            IEventBus<DestinyUserProfileBroadcastDbModel> profileBroadcastEventChannel,
            IDiscordClientProvider discordClientProvider,
            IDestinyDb destinyDb,
            BroadcastSaver broadcastSaver,
            IBungieClientProvider bungieClientProvider) : base(logger)
        {
            _logger = logger;
            _clanBroadcastEventChannel = clanBroadcastEventChannel;
            _profileBroadcastEventChannel = profileBroadcastEventChannel;
            _discordClientProvider = discordClientProvider;
            _destinyDb = destinyDb;
            _bungieClientProvider = bungieClientProvider;
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

            var clanEmbed = Embeds.Broadcasts.Clan(clanBroadcast, clanModel);

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
                await SendUserBroadcast(broadcasts.First(), cancellationToken);
                return;
            }

            var userNamesKeyedByMembershipId = new Dictionary<long, string>(amountOfBroadcasts);
            foreach (var broadcast in broadcasts)
            {
                var userName = await _dbContextCaller.GetDestinyUserDisplayNameAsync(
                    broadcast.MembershipId,
                    cancellationToken);

                if (string.IsNullOrEmpty(userName))
                    continue;

                userNamesKeyedByMembershipId.Add(broadcast.MembershipId, userName);
            }

            var clanIds = broadcasts
                .DistinctBy(x => x.ClanId)
                .Select(x => x.ClanId);

            var clansData = new List<ClanDbModel>();

            foreach (var clanId in clanIds)
            {
                var clanData = await _dbContextCaller.GetClanAsync(clanId, cancellationToken);
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
                    await _dbContextCaller.MarkUserBroadcastSent(broadcast, cancellationToken);
                    _logger.LogWarning("Failed to send broadcast due to guild being null {@Broadcast}", broadcast);
                }
                return;
            }

            var channelId = await _dbContextCaller.GetGuildBroadcastChannelAsync(guildId,
                cancellationToken);

            if (channelId is null)
            {

                foreach (var broadcast in broadcasts)
                {
                    await _dbContextCaller.MarkUserBroadcastSent(broadcast, cancellationToken);
                    _logger.LogWarning("Failed to send broadcast due to channelId being {@Broadcast}", broadcast);
                }
                return;
            }

            var channel = guild.GetTextChannel(channelId.Value);

            if (channel is not null)
            {
                var embed = Embeds.Broadcasts.BuildDestinyUserGroupedBroadcast(
                    broadcasts,
                    broadcastType,
                    definitionHash,
                    clansDictionary,
                    bungieClient,
                    userNamesKeyedByMembershipId);

                await channel.SendMessageAsync(embed: embed);

                foreach (var broadcast in broadcasts)
                    await _dbContextCaller.MarkUserBroadcastSent(broadcast, cancellationToken);
            }
            else
            {
                foreach (var broadcast in broadcasts)
                {
                    await _dbContextCaller.MarkUserBroadcastSent(broadcast, cancellationToken);
                    _logger.LogWarning("Failed to send broadcast due to channel being null {@Broadcast}", broadcast);
                }
                return;
            }
        }

        private IEnumerable<T> TryDequeue<T>(ConcurrentQueue<T> source)
        {
            while (source.TryDequeue(out var item))
            {
                yield return item;
            }
        }
    }
}
