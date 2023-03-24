using Atheon.Services.DiscordHandlers.Autocompleters.DestinyMetrics;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using Atheon.Extensions;
using DotNetBungieAPI.Extensions;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny.Profiles;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("leaderboards", "Group of commands to show leaderboards for Destiny 2")]
public class LeaderboardsCommandHandler : SlashCommandHandlerBase
{
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly ILocalizationService _localizationService;

    public LeaderboardsCommandHandler(
        ILogger<LeaderboardsCommandHandler> logger,
        EmbedBuilderService embedBuilderService,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider,
        IMemoryCache memoryCache,
        ILocalizationService localizationService) : base(logger, embedBuilderService)
    {
        _embedBuilderService = embedBuilderService;
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _memoryCache = memoryCache;
        _localizationService = localizationService;
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

            var lang = await _localizationService.GetGuildLocale(GuildId);

            bungieClient.TryGetDefinition<DestinyMetricDefinition>(metricHash, lang, out var metricDefinition);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetProfileMetricsAsync(metricHash, !metricDefinition.LowerValueIsBetter, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"{metricDefinition.DisplayProperties.Name} Leaderboard")
                .WithThumbnailUrl(metricDefinition.DisplayProperties.Icon.AbsolutePath);

            var getters = new Func<DestinyProfileLiteWithValue<int>, object>[]
            {
                user => user.Name,
                user =>
                {
                    return user.Value.FormatUIDisplayValue(metricDefinition.TrackingObjective.GetValueOrNull());
                }
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters, 
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("guardian-ranks", "Shows leaderboard for guardian ranks")]
    public async Task CreateLeaderboardForGuardianRanksAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetGuardianRanksLeaderboardAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"Guardian Ranks Leaderboard");

            var getters = new Func<DestinyProfileLiteWithValue<int>, object>[]
            {
                user => user.Name,
                user =>
                {
                    return user.Value;
                }
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("power", "Shows leaderboard for power")]
    public async Task CreateLeaderboardForPowerLevelAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetGuardianPowerLevelAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"Power Level Leaderboard");

            var getters = new Func<DestinyProfileLiteWithDoubleValues<int, int>, object>[]
            {
                user => user.Name,
                user => user.FirstValue + user.SecondValue,
                user => user.FirstValue,
                user => user.SecondValue
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithDoubleValues<int, int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters, 
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("triumph-score", "Shows leaderboard for triumph score")]
    public async Task CreateLeaderboardForTriumphScoreAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetGuardianTriumphScoreAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"Triumph Score Leaderboard");

            var getters = new Func<DestinyProfileLiteWithDoubleValues<int, int>, object>[]
            {
                user => user.Name,
                user => user.FirstValue + user.SecondValue,
                user => user.FirstValue,
                user => user.SecondValue
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithDoubleValues<int, int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("time-played", "Shows leaderboard for time played")]
    public async Task CreateLeaderboardForTimePlayedAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetTimePlayedLeaderboardAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"Time played Leaderboard");

            var getters = new Func<DestinyProfileLiteWithValue<int>, object>[]
            {
                user => user.Name,
                user =>
                {
                    return TimeSpan.FromMinutes(user.Value).ToString("c");
                }
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("triumph", "Shows leaderboard for triumph progress")]
    public async Task CreateLeaderboardForTriumphProgress(
        [Autocomplete(typeof(DestinyLeaderboardValidRecordDefinitionAutocompleter))][Summary("triumph", "Triumph")] string triumphHashString,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var recordHash = uint.Parse(triumphHashString);
            var bungieClient = await _bungieClientProvider.GetClientAsync();

            var lang = await _localizationService.GetGuildLocale(GuildId);

            bungieClient.TryGetDefinition<DestinyRecordDefinition>(recordHash, lang, out var recordDefinition);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);

            List<DestinyProfileLiteWithValue<int>> users;

            if (recordDefinition.IntervalInfo.IntervalObjectives.Count > 0)
            {
                users = await _destinyDb.GetRecordIntervalObjectiveLeaderboardAsync(recordHash, guildSettings.Clans.ToArray());
            }
            else
            {
                users = await _destinyDb.GetRecordObjectiveLeaderboardAsync(recordHash, guildSettings.Clans.ToArray());
            }

            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithDescription(recordDefinition.DisplayProperties.Description)
                .WithTitle($"{recordDefinition.DisplayProperties.Name} Leaderboard")
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath);

            var getters = new Func<DestinyProfileLiteWithValue<int>, object>[]
            {
                user => user.Name,
                user => user.Value
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("titles", "Shows leaderboard for total titles")]
    public async Task CreateLeaderboardForTotalTitlesAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetTotalTitlesLeaderboardAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"Total Titles Leaderboard");

            var getters = new Func<DestinyProfileLiteWithValue<int>, object>[]
            {
                user => user.Name,
                user =>
                {
                    return user.Value;
                }
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            await Context.Interaction.RespondAsync(
                embed: embedBuilder.Build(),
                ephemeral: hide);
        });
    }
}
