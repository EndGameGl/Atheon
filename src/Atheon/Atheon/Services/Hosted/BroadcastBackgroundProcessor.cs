using Atheon.Models.Collections;
using Atheon.Models.Database.Destiny.Broadcasts;
using Atheon.Models.Database.Destiny.Interfaces;
using Atheon.Services.DiscordHandlers.EmbedBuilders;
using Atheon.Services.EventBus;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace Atheon.Services.Hosted
{
    public class BroadcastBackgroundProcessor : PeriodicBackgroundService
    {
        private readonly IEventBus<ClanBroadcastDbModel> _clanBroadcastEventChannel;
        private readonly IEventBus<DestinyUserProfileBroadcastDbModel> _profileBroadcastEventChannel;
        private readonly IDiscordClientProvider _discordClientProvider;
        private readonly IDestinyDb _destinyDb;
        private ConcurrentQueue<ClanBroadcastDbModel> _clanBroadcasts = new();
        private ConcurrentQueue<DestinyUserProfileBroadcastDbModel> _userBroadcasts = new();

        public BroadcastBackgroundProcessor(
            ILogger<BroadcastBackgroundProcessor> logger,
            IEventBus<ClanBroadcastDbModel> clanBroadcastEventChannel,
            IEventBus<DestinyUserProfileBroadcastDbModel> profileBroadcastEventChannel,
            IDiscordClientProvider discordClientProvider,
            IDestinyDb destinyDb) : base(logger)
        {
            _clanBroadcastEventChannel = clanBroadcastEventChannel;
            _profileBroadcastEventChannel = profileBroadcastEventChannel;
            _discordClientProvider = discordClientProvider;
            _destinyDb = destinyDb;
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

            foreach (var clanBroadcast in TryDequeue(_clanBroadcasts))
            {
                await ProcessClanBrocast(client, clanBroadcast);
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

        private IEnumerable<T> TryDequeue<T>(ConcurrentQueue<T> source)
        {
            while (source.TryDequeue(out var item))
            {
                yield return item;
            }
        }
    }
}
