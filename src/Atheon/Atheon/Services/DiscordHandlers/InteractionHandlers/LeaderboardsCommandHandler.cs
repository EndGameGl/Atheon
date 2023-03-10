using Atheon.Services.DiscordHandlers.Autocompleters.DestinyMetrics;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using System.Text;
using Atheon.Models.Database.Destiny.Profiles;
using static DotNetBungieAPI.HashReferences.DefinitionHashes;
using Atheon.Extensions;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("leaderboards", "Group of commands to show leaderboards for Destiny 2")]
public class LeaderboardsCommandHandler : SlashCommandHandlerBase
{
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;

    public LeaderboardsCommandHandler(
        ILogger<LeaderboardsCommandHandler> logger,
        EmbedBuilderService embedBuilderService,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider) : base(logger, embedBuilderService)
    {
        _embedBuilderService = embedBuilderService;
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
    }

    [SlashCommand("metric", "Shows leaderboard for a certain metric")]
    public async Task CreateLeaderboardForMetricAsync(
        [Autocomplete(typeof(DestinyMetricDefinitionAutocompleter))][Summary("metric", "Metric to show")] string metricHashString,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var metricHash = uint.Parse(metricHashString);
            var bungieClient = await _bungieClientProvider.GetClientAsync();
            bungieClient.TryGetDefinition<DestinyMetricDefinition>(metricHash, BungieLocales.EN, out var metricDefinition);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetProfileMetricsAsync(metricHash, !metricDefinition.LowerValueIsBetter, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"{metricDefinition.DisplayProperties.Name} Leaderboard")
                .WithThumbnailUrl(metricDefinition.DisplayProperties.Icon.AbsolutePath);

            var getters = new Func<DestinyProfileLiteWithValue, object>[]
            {
                user => user.Name,
                user => user.Value
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters)
                .LimitTo(1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }
}
