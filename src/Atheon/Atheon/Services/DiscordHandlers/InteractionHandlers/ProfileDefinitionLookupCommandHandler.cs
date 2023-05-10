using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny.Profiles;
using Atheon.Destiny2.Metadata;
using Atheon.Extensions;
using Atheon.Models.DiscordModels;
using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.PresentationNodes;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("lookup", "Set of commands to check user statuses")]
public class ProfileDefinitionLookupCommandHandler : SlashCommandHandlerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly ILocalizationService _localizationService;

    public ProfileDefinitionLookupCommandHandler(
        ILogger<ProfileDefinitionLookupCommandHandler> logger,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider,
        EmbedBuilderService embedBuilderService,
        DestinyDefinitionDataService destinyDefinitionDataService,
        ILocalizationService localizationService) : base(logger, embedBuilderService)
    {
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _embedBuilderService = embedBuilderService;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _localizationService = localizationService;
    }

    [SlashCommand("item-check", "Checks who has items")]
    public async Task GetUsersWithItem(
        [Autocomplete(typeof(DestinyCollectibleDefinitionAutocompleter))][Summary(description: "Collectible")] string collectibleHashString,
        [Summary(description: "Whether user has item or not")] bool hasItem,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(collectibleHashString, out var collectibleHash))
                return Error($"Failed to parse collectible hash");

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetProfilesCollectibleStatusAsync(collectibleHash, hasItem, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);
            var bungieClient = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, lang, out var colDef))
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(collectibleHash);

            var (defName, defIcon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(colDef, lang);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"{users.Count} users {(hasItem ? "have" : "miss")} {defName}")
                .WithThumbnailUrl(defIcon);

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var sb = new StringBuilder();
                sb.Append("```");
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                for (int i = 0; i < usersOfClan.Count; i++)
                {
                    var user = usersOfClan[i];
                    if (user.Name is "#")
                        continue;
                    var userDisplayString = $"{user.Name}\n";
                    if ((sb.Length + userDisplayString.Length) <= 1005)
                    {
                        sb.Append(userDisplayString);
                    }
                    else
                    {
                        var left = usersOfClan.Count - i + 1;
                        sb.Append($"And {left} more...");
                        break;
                    }
                }
                sb.Append("```");
                embedBuilder.AddField(reference.Name, sb.ToString(), j % 2 == 0);
            }

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("triumph-check", "Checks who completed triumph")]
    public async Task GetUsersWithRecord(
        [Autocomplete(typeof(DestinyRecordDefinitionAutocompleter))][Summary(description: "Record")] string recordHashString,
        [Summary(description: "Whether user has completed triumph or not")] bool hasCompletedTriumph,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(recordHashString, out var recordHash))
                return Error($"Failed to parse record hash");

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetProfilesRecordStatusAsync(recordHash, hasCompletedTriumph, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);
            var bungieClient = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!bungieClient.TryGetDefinition<DestinyRecordDefinition>(recordHash, lang, out var recordDefinition))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath)
                .WithTitle($"{users.Count} users have{(hasCompletedTriumph ? " " : " not ")}completed {recordDefinition.DisplayProperties.Name}");

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var sb = new StringBuilder();
                sb.Append("```");
                var usersOfClan = users.Where(x => x.ClanId == reference.Id).ToList();

                for (int i = 0; i < usersOfClan.Count; i++)
                {
                    var user = usersOfClan[i];
                    if (user.Name is "#")
                        continue;
                    var userDisplayString = $"{user.Name}\n";
                    if ((sb.Length + userDisplayString.Length) <= 1005)
                    {
                        sb.Append(userDisplayString);
                    }
                    else
                    {
                        var left = usersOfClan.Count - i + 1;
                        sb.Append($"And {left} more...");
                        break;
                    }
                }
                sb.Append("```");
                embedBuilder.AddField(reference.Name, sb.ToString(), j % 2 == 0);
            }

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("drystreaks", "Checks who don't have item YET")]
    public async Task GetUserDrystreaks(
        [Summary("item", "Item to look for")]
        [Choice("One Thousand Voices", "199171385")]
        [Choice("Eyes of Tomorrow", "753200559")]
        [Choice("Vex Mythoclast", "2300465938")]
        [Choice("Collective Obligation", "2817568609")]
        [Choice("Heartshadow", "467760883")]
        [Choice("In Memoriam Shell", "2298387876")]
        [Choice("Touch of Malice", "192937277")]
        [Choice("Hierarchy of Needs", "3558330464")]
        [Choice("Conditional Finality", "2553509474")]
        uint collectibleHash,
        [Summary(description: "Whether to hide this message")]
        bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, lang, out var collectibleDefinition))
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(collectibleHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var drystreaks = await _destinyDb.GetProfileDrystreaksAsync(collectibleHash, guildSettings.Clans.ToArray());
            var clanIds = drystreaks.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithThumbnailUrl(collectibleDefinition.DisplayProperties.Icon.AbsolutePath)
                .WithTitle($"Users who don't have {collectibleDefinition.DisplayProperties.Name}");

            if (Destiny2Metadata.DryStreakItemSources.TryGetValue(collectibleHash, out var source))
            {
                embedBuilder.WithDescription(source);
            }

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = drystreaks.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    new Func<DestinyProfileLiteWithValue<int>, object>[]
                    {
                        user => user.Name,
                        user => user.Value
                    },
                    limit: 1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("title", "Checks who completed title")]
    public async Task GetUserTitles(
        [Summary("title", "Title to look for")][Autocomplete(typeof(DestinyTitleAutocompleter))] string titleRecordHashString,
        [Summary(description: "Whether user has title or not")] bool hasTitle,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(titleRecordHashString, out var titleRecordHash))
                return Error($"Failed to parse record hash");

            var client = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyRecordDefinition>(titleRecordHash, lang, out var titleDefinition))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(titleRecordHash);


            var titleName = titleDefinition.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine];
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var titles = await _destinyDb.GetProfileTitlesAsync(titleRecordHash, hasTitle, guildSettings.Clans.ToArray());
            var clanIds = titles.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
            .GetTemplateEmbed()
                .WithTitle($"Users who have {titleName} title");

            var gettersList = new List<Func<DestinyProfileLiteWithValue<int>, object>>()
            {
                user => user.Name
            };

            if (hasTitle)
            {
                gettersList.Add(user => user.Value);
            }

            var getters = gettersList.ToArray();

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = titles.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLiteWithValue<int>, long>(
                    usersOfClan.Count,
                    "No users",
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            var icon = string.Empty;

            if (titleDefinition.DisplayProperties.Icon.HasValue)
            {
                icon = titleDefinition.DisplayProperties.Icon.AbsolutePath;
            }
            else
            {
                var parentNode = client.Repository
                     .GetAll<DestinyPresentationNodeDefinition>()
                     .FirstOrDefault(x => x.CompletionRecord == titleDefinition.Hash);

                icon = parentNode?.DisplayProperties.Icon.AbsolutePath;
            }
            embedBuilder.WithThumbnailUrl(icon ?? titleDefinition.DisplayProperties.Icon.AbsolutePath);

            return Success(embedBuilder.Build(), hide);
        });
    }

    [SlashCommand("game-version", "Checks who has game version")]
    public async Task GetUsersWithGameVersion(
        [Summary("game-version", "Game version to look up")] DestinyGameVersionEnum gameVersion,
        [Summary(description: "Whether user has title or not")] bool hasVersion,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var lang = await _localizationService.GetGuildLocale(GuildId);
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var playersWithGameVersion = await _destinyDb.GetPlayersWithGameVersionAsync(gameVersion.ConvertToDestinyGameVersion(), hasVersion, guildSettings.Clans.ToArray());
            var clanIds = playersWithGameVersion.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
            .GetTemplateEmbed()
                .WithTitle($"Users who {(hasVersion is false ? "don't " : "")}have {gameVersion}");

            var gettersList = new List<Func<DestinyProfileLite, object>>()
            {
                user => user.Name
            };

            var getters = gettersList.ToArray();

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var usersOfClan = playersWithGameVersion.Where(x => x.ClanId == reference.Id).ToList();

                var formattedData = _embedBuilderService.FormatAsStringTable<DestinyProfileLite, long>(
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
