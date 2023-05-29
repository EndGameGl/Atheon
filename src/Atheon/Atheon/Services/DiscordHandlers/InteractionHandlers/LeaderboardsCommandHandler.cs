using Atheon.Services.DiscordHandlers.Autocompleters.DestinyMetrics;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using DotNetBungieAPI.Extensions;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinySeasonPasses;
using DotNetBungieAPI.Models.Destiny.Definitions.SeasonPasses;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("leaderboards", "Group of commands to show leaderboards for Destiny 2")]
public class LeaderboardsCommandHandler : SlashCommandHandlerBase
{
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILocalizationService _localizationService;

    public LeaderboardsCommandHandler(
        ILogger<LeaderboardsCommandHandler> logger,
        EmbedBuilderService embedBuilderService,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider,
        ILocalizationService localizationService) : base(logger, embedBuilderService)
    {
        _embedBuilderService = embedBuilderService;
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _localizationService = localizationService;
    }

    [SlashCommand("metric", "Shows leaderboard for a certain metric")]
    public async Task CreateLeaderboardForMetricAsync(
        [Autocomplete(typeof(DestinyMetricDefinitionAutocompleter))][Summary("metric", "Metric to show")] string metricHashString,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(metricHashString, out var metricHash))
                return Error($"Couldn't parse metric hash");

            var bungieClient = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);
            if (!bungieClient.TryGetDefinition<DestinyMetricDefinition>(metricHash, out var metricDefinition, lang))
                return DestinyDefinitionNotFound<DestinyMetricDefinition>(metricHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("guardian-ranks", "Shows leaderboard for guardian ranks")]
    public async Task CreateLeaderboardForGuardianRanksAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("power", "Shows leaderboard for power")]
    public async Task CreateLeaderboardForPowerLevelAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("triumph-score", "Shows leaderboard for triumph score")]
    public async Task CreateLeaderboardForTriumphScoreAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("time-played", "Shows leaderboard for time played")]
    public async Task CreateLeaderboardForTimePlayedAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("triumph", "Shows leaderboard for triumph progress")]
    public async Task CreateLeaderboardForTriumphProgress(
        [Autocomplete(typeof(DestinyLeaderboardValidRecordDefinitionAutocompleter))][Summary("triumph", "Triumph")] string triumphHashString,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(triumphHashString, out var recordHash))
                return Error("Failed to parse triumph hash");

            var bungieClient = await _bungieClientProvider.GetClientAsync();

            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!bungieClient.TryGetDefinition<DestinyRecordDefinition>(recordHash, out var recordDefinition, lang))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("titles", "Shows leaderboard for total titles")]
    public async Task CreateLeaderboardForTotalTitlesAsync(
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();
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

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("season-pass", "Shows leaderboard for selected season")]
    public async Task CreateLeaderboardForSeasonPassLevelAsync(
        [Autocomplete(typeof(DestinySeasonPassDefinitionAutocompleter))][Summary("season", "Season pass")] string seasonPassHashString,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(seasonPassHashString, out var seasonPassHash))
                return Error("Failed to parse season pass hash");

            var bungieClient = await _bungieClientProvider.GetClientAsync();

            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!bungieClient.TryGetDefinition<DestinySeasonPassDefinition>(seasonPassHash, out var seasonPassDefinition, lang))
                return DestinyDefinitionNotFound<DestinySeasonPassDefinition>(seasonPassHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
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
                .WithTitle($"Season Pass Levels Leaderboard")
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
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide);
        });
    }
}
