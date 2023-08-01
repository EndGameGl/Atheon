using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyMetrics;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinySeasonPasses;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using DotNetBungieAPI.Models.Destiny.Definitions.SeasonPasses;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("leaderboards", "Group of commands to show leaderboards for Destiny 2")]
public class LeaderboardsCommandHandler : LocalizedSlashCommandHandler
{
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly IGuildDb _guildDb;

    public LeaderboardsCommandHandler(
        ILogger<LeaderboardsCommandHandler> logger,
        EmbedBuilderService embedBuilderService,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider,
        ILocalizationService localizationService,
        IGuildDb guildDb) : base(localizationService, logger, embedBuilderService)
    {
        _embedBuilderService = embedBuilderService;
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _guildDb = guildDb;
    }

    [SlashCommand("metric", "Shows leaderboard for a certain metric")]
    public async Task CreateLeaderboardForMetricAsync(
        [Autocomplete(typeof(DestinyMetricDefinitionAutocompleter))][Summary("metric", "Metric to show")] string metricHashString,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(metricHashString, out var metricHash))
                return Error(FormatText("FailedToParseMetricHashError", () => "Failed to parse metric hash: {0}", metricHashString));

            var bungieClient = await _bungieClientProvider.GetClientAsync();
            if (!bungieClient.TryGetDefinition<DestinyMetricDefinition>(metricHash, out var metricDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyMetricDefinition>(metricHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetProfileMetricsAsync(metricHash, !metricDefinition.LowerValueIsBetter, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(FormatText("MetricLeaderboardTitle", () => "{0} Leaderboard", metricDefinition.DisplayProperties.Name))
                .WithThumbnailUrl(metricDefinition.DisplayProperties.Icon.AbsolutePath);

            var getters = new Func<DestinyProfileLiteWithValue<int>, object>[]
            {
                user => user.Name,
                user =>
                {
                    var objective = metricDefinition.TrackingObjective.GetValueOrNull(GuildLocale);
                    return user.Value.FormatUIDisplayValue(objective);
                }
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("guardian-ranks", "Shows leaderboard for guardian ranks")]
    public async Task CreateLeaderboardForGuardianRanksAsync(
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetGuardianRanksLeaderboardAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(Text("GuardianRanksLeaderboardTitle", () => "Guardian Ranks Leaderboard"));

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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("power", "Shows leaderboard for power")]
    public async Task CreateLeaderboardForPowerLevelAsync(
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetGuardianPowerLevelAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(Text("PowerLevelLeaderboardTitle", () => "Power Level Leaderboard"));

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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("triumph-score", "Shows leaderboard for triumph score")]
    public async Task CreateLeaderboardForTriumphScoreAsync(
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetGuardianTriumphScoreAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(Text("TriumphScoreLeaderboard", () => "Triumph Score Leaderboard"));

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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("time-played", "Shows leaderboard for time played")]
    public async Task CreateLeaderboardForTimePlayedAsync(
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetTimePlayedLeaderboardAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(Text("TimePlayedLeaderboard", () => "Time played Leaderboard"));

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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("triumph", "Shows leaderboard for triumph progress")]
    public async Task CreateLeaderboardForTriumphProgress(
        [Autocomplete(typeof(DestinyLeaderboardValidRecordDefinitionAutocompleter))][Summary("triumph", "Triumph")] string triumphHashString,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(triumphHashString, out var recordHash))
                return Error(FormatText("FailedToParseRecordHashError", () => "Failed to parse record hash: {0}", triumphHashString));

            var bungieClient = await _bungieClientProvider.GetClientAsync();

            if (!bungieClient.TryGetDefinition<DestinyRecordDefinition>(recordHash, out var recordDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

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
                .WithTitle(FormatText("TriumphProgressLeaderboard", () => "{0} Leaderboard", recordDefinition.DisplayProperties.Name))
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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("titles", "Shows leaderboard for total titles")]
    public async Task CreateLeaderboardForTotalTitlesAsync(
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();
            var users = await _destinyDb.GetTotalTitlesLeaderboardAsync(guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(FormatText("TotalTitlesLeaderboardTitle", () => "Total Titles Leaderboard"));

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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("season-pass", "Shows leaderboard for selected season")]
    public async Task CreateLeaderboardForSeasonPassLevelAsync(
        [Autocomplete(typeof(DestinySeasonPassDefinitionAutocompleter))][Summary("season", "Season pass")] string seasonPassHashString,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(seasonPassHashString, out var seasonPassHash))
                return Error(FormatText("FailedToParseSeasonPassHashError", () => "Failed to parse season pass hash: {0}", seasonPassHashString));

            var bungieClient = await _bungieClientProvider.GetClientAsync();
            if (!bungieClient.TryGetDefinition<DestinySeasonPassDefinition>(seasonPassHash, out var seasonPassDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinySeasonPassDefinition>(seasonPassHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetGuardianSeasonPassLevelsAsync(
                seasonPassDefinition.RewardProgression.Hash.GetValueOrDefault(),
                seasonPassDefinition.PrestigeProgression.Hash.GetValueOrDefault(),
                guildSettings.Clans.ToArray());

            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(Text("SeasonPassLevelsLeaderboardTitle", () => "Season Pass Levels Leaderboard"))
                .WithDescription(seasonPassDefinition.DisplayProperties.Name);

            var getters = new Func<DestinyProfileLiteWithDoubleValues<int, int>, object>[]
            {
                user => user.Name,
                user => user.FirstValue + user.SecondValue
            };

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithDoubleValues<int, int>, long>(
                    usersOfClan.Count,
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }
}
