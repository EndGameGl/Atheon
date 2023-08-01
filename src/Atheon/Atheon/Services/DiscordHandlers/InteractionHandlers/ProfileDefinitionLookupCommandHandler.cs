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
public class ProfileDefinitionLookupCommandHandler : LocalizedSlashCommandHandler
{
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IGuildDb _guildDb;

    public ProfileDefinitionLookupCommandHandler(
        ILogger<ProfileDefinitionLookupCommandHandler> logger,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider,
        EmbedBuilderService embedBuilderService,
        DestinyDefinitionDataService destinyDefinitionDataService,
        ILocalizationService localizationService,
        IGuildDb guildDb) : base(localizationService, logger, embedBuilderService)
    {
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _embedBuilderService = embedBuilderService;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _guildDb = guildDb;
    }

    [SlashCommand("item-check", "Checks who has items")]
    public async Task GetUsersWithItem(
        [Autocomplete(typeof(DestinyCollectibleDefinitionAutocompleter))][Summary("item", "Item to look for")] string collectibleHashString,
        [Summary("has-item", "Whether user has item or not")] bool hasItem,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(collectibleHashString, out var collectibleHash))
                return Error(FormatText("FailedToParseCollectibleHashError", () => "Failed to parse collectible hash: {0}", collectibleHashString));

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetProfilesCollectibleStatusAsync(collectibleHash, hasItem, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);
            var bungieClient = await _bungieClientProvider.GetClientAsync();

            if (!bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, out var colDef, GuildLocale))
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(collectibleHash);

            var (defName, defIcon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(colDef, GuildLocale);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(hasItem ?
                    FormatText("UsersWithItemCount", () => "{0} users have {1}", users.Count, defName) :
                    FormatText("UsersWithoutItemCount", () => "{0} users miss {1}", users.Count, defName))
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
                        sb.Append(FormatText("HiddenUsersCount", () => "And {0} more...", left));
                        break;
                    }
                }
                sb.Append("```");
                embedBuilder.AddField(reference.Name, sb.ToString(), j % 2 == 0);
            }

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("triumph-check", "Checks who completed triumph")]
    public async Task GetUsersWithRecord(
        [Autocomplete(typeof(DestinyRecordDefinitionAutocompleter))][Summary("triumph", "Triumph to look for")] string recordHashString,
        [Summary("has-completed-triumph", "Whether user has completed triumph or not")] bool hasCompletedTriumph,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(recordHashString, out var recordHash))
                return Error(FormatText("FailedToParseRecordHashError", () => "Failed to parse record hash: {0}", recordHashString));

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var users = await _destinyDb.GetProfilesRecordStatusAsync(recordHash, hasCompletedTriumph, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);
            var bungieClient = await _bungieClientProvider.GetClientAsync();

            if (!bungieClient.TryGetDefinition<DestinyRecordDefinition>(recordHash, out var recordDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath)
                .WithTitle(hasCompletedTriumph ?
                    FormatText("UsersWithTriumphCount", () => "{0} users have completed {1}", users.Count, recordDefinition.DisplayProperties.Name) :
                    FormatText("UsersWithoutTriumphCount", () => "{0} users have not completed {1}", users.Count, recordDefinition.DisplayProperties.Name));

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
                        sb.Append(FormatText("HiddenUsersCount", () => "And {0} more...", left));
                        break;
                    }
                }
                sb.Append("```");
                embedBuilder.AddField(reference.Name, sb.ToString(), j % 2 == 0);
            }

            return Success(embedBuilder.Build(), hide: hide);
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
        [Choice("The Navigator", "161963863")]
        uint collectibleHash,
        [Summary("hide", "Whether to hide this message")]
        bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, out var collectibleDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(collectibleHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var drystreaks = await _destinyDb.GetProfileDrystreaksAsync(collectibleHash, guildSettings.Clans.ToArray());
            var clanIds = drystreaks.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithThumbnailUrl(collectibleDefinition.DisplayProperties.Icon.AbsolutePath)
                .WithTitle(FormatText(
                    "UsersWithoutItemCount",
                    () => "{0} users miss {1}",
                    drystreaks.Count,
                    collectibleDefinition.DisplayProperties.Name));

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
                    Text("NoUsersFound", () => "No users"),
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

            return Success(embedBuilder.Build(), hide: hide);
        });
    }

    [SlashCommand("title", "Checks who completed title")]
    public async Task GetUserTitles(
        [Summary("title", "Title to look for")][Autocomplete(typeof(DestinyTitleAutocompleter))] string titleRecordHashString,
        [Summary("has-title", "Whether user has title or not")] bool hasTitle,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(titleRecordHashString, out var titleRecordHash))
                return Error(FormatText("FailedToParseRecordHashError", () => "Failed to parse record hash: {0}", titleRecordHashString));

            var client = await _bungieClientProvider.GetClientAsync();

            if (!client.TryGetDefinition<DestinyRecordDefinition>(titleRecordHash, out var titleDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(titleRecordHash);


            var titleName = titleDefinition.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine];
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var titles = await _destinyDb.GetProfileTitlesAsync(titleRecordHash, hasTitle, guildSettings.Clans.ToArray());
            var clanIds = titles.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(FormatText("UsersWithTitleTitle", () => "Users who have {0} title", titleName));

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
                    Text("NoUsersFound", () => "No users"),
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

            return Success(embedBuilder, hide: hide);
        });
    }

    [SlashCommand("game-version", "Checks who has game version")]
    public async Task GetUsersWithGameVersion(
        [Summary("game-version", "Game version to look up")] DestinyGameVersionEnum gameVersion,
        [Summary("has-version", "Whether user has game version or not")] bool hasVersion,
        [Summary("hide", "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            var playersWithGameVersion = await _destinyDb.GetPlayersWithGameVersionAsync(gameVersion.ConvertToDestinyGameVersion(), hasVersion, guildSettings.Clans.ToArray());
            var clanIds = playersWithGameVersion.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle(hasVersion is false ?
                    FormatText("UsersWithoutGameVersion", () => "Users who don't have {0}", gameVersion) :
                    FormatText("UsersWithGameVersion", () => "Users who have {0}", gameVersion));

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
                    Text("NoUsersFound", () => "No users"),
                    usersOfClan,
                    (user) => user.MembershipId,
                    getters,
                    1018);

                embedBuilder.AddField(reference.Name, $"```{formattedData}```");
            }

            return Success(embedBuilder, hide: hide);
        });
    }
}
