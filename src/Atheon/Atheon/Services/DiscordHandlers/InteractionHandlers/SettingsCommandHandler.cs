using Atheon.DataAccess;
using Atheon.DataAccess.Models.Administration;
using Atheon.DataAccess.Models.Destiny.Links;
using Atheon.DataAccess.Models.Discord;
using Atheon.Extensions;
using Atheon.Models.Destiny;
using Atheon.Models.DiscordModels;
using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers.Autocompleters;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyPresentationNodes;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.DiscordHandlers.Preconditions;
using Atheon.Services.Interfaces;
using Atheon.Services.Localization;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.PresentationNodes;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("settings", "Group of commands related to guild settings")]
public class SettingsCommandHandler : SlashCommandHandlerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IClanQueue _clanQueue;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly ILocalizationService _localizationService;

    public SettingsCommandHandler(
        ILogger<SettingsCommandHandler> logger,
        IDestinyDb destinyDb,
        IClanQueue clanQueue,
        EmbedBuilderService embedBuilderService,
        IBungieClientProvider bungieClientProvider,
        DestinyDefinitionDataService destinyDefinitionDataService,
        ILocalizationService localizationService) : base(logger, embedBuilderService)
    {
        _destinyDb = destinyDb;
        _clanQueue = clanQueue;
        _embedBuilderService = embedBuilderService;
        _bungieClientProvider = bungieClientProvider;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _localizationService = localizationService;
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-add", "Adds new clan to guild")]
    public async Task AddClanToGuildAsync(
        [Autocomplete(typeof(DestinyClanByIdAutocompleter)), Summary("Clan")] long clanId)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var settings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (settings is null)
                return GuildSettingsNotFound();

            if (settings.Clans.Contains(clanId))
            {
                var existingGuildClan = await _destinyDb.GetClanModelAsync(clanId);
                if (existingGuildClan is null)
                    return Error($"Clan {clanId} not found in database");

                existingGuildClan.IsTracking = true;
                await _destinyDb.UpsertClanModelAsync(existingGuildClan);

                var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed("Add new clan failed", "Clan is already added to this discord guild");
                return Success(embedTemplate.Build());
            }

            var existingClan = await _destinyDb.GetClanModelAsync(clanId);

            if (existingClan is not null)
            {
                settings.Clans.Add(clanId);
                await _destinyDb.UpsertGuildSettingsAsync(settings);
                var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed("Add new clan success", "Added clan to this guild");
                return Success(embedTemplate.Build());
            }

            _clanQueue.EnqueueFirstTimeScan(clanId);
            settings.Clans.Add(clanId);
            await _destinyDb.UpsertGuildSettingsAsync(settings);
            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Add new clan success",
                    "New clan will be ready when scan message pops up")
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-remove", "Removes selected clan from guild")]
    public async Task RemoveClanFromGuildAsync(
        [Autocomplete(typeof(DestinyClanFromGuildAutocompleter)), Summary("Clan")] long clanIdToRemove)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(Context.Guild.Id);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.Clans.Remove(clanIdToRemove);
            var clanModel = await _destinyDb.GetClanModelAsync(clanIdToRemove);
            if (clanModel is null)
                return Error("Clan not found");

            clanModel.IsTracking = false;

            await _destinyDb.UpsertClanModelAsync(clanModel);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed($"Removed clan {clanIdToRemove}", "Success").Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("link-user", "Links discord user to destiny profile")]
    public async Task LinkUserAsync(
        [Autocomplete(typeof(SearchDestinyUserByNameAutocompleter))][Summary("User")] DestinyProfilePointer user,
        [Summary("link-to", "User to link to")] IUser? linkTo = null)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var userToLinkTo = linkTo is null ? Context.User : linkTo;

            var link = new DiscordToDestinyProfileLink()
            {
                DiscordUserId = userToLinkTo.Id,
                DestinyMembershipId = user.MembershipId,
                BungieMembershipType = user.MembershipType
            };

            await _destinyDb.UpsertProfileLinkAsync(link);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "User updated",
                    $"{userToLinkTo.Mention} is now linked to {user.MembershipId}").Build());
        });
    }

    [RequireOwner]
    [SlashCommand("admin-add", "Links discord user to destiny profile")]
    public async Task AddServerAdminAsync(
        [Summary("user", "New server admin")] IUser user)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await _destinyDb.AddServerAdministratorAsync(new ServerBotAdministrator()
            {
                DiscordGuildId = Context.Guild.Id,
                DiscordUserId = user.Id
            });

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Admin added",
                    $"{user.Mention} is now bot admin").Build());
        });
    }

    [RequireOwner]
    [SlashCommand("admin-remove", "Links discord user to destiny profile")]
    public async Task RemoveServerAdminAsync(
        [Summary("user", "Server admin to remove")] IUser user)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await _destinyDb.RemoveServerAdministratorAsync(new ServerBotAdministrator()
            {
                DiscordGuildId = Context.Guild.Id,
                DiscordUserId = user.Id
            });

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Admin removed",
                    $"{user.Mention} is not bot admin anymore").Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("set-report-channel", "Sets report channel")]
    public async Task SetReportChannelAsync(
        [Summary("channel", "Channel to send reports to")][ChannelTypes(ChannelType.Text)] IChannel channel)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.DefaultReportChannel = channel.Id;
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Default report channel is now <#{channel.Id}>")
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("set-report-settings", "Sets report settings")]
    public async Task SetReportSettingsAsync(
        [Summary(description: "Whether to report new items")] DiscordNullableBoolean reportItems = DiscordNullableBoolean.None,
        [Summary(description: "Whether to report completed triumphs")] DiscordNullableBoolean reportTriumphs = DiscordNullableBoolean.None)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            if (reportItems != DiscordNullableBoolean.None)
            {
                guildSettings.TrackedCollectibles.IsReported = reportItems is DiscordNullableBoolean.True;
            }

            if (reportTriumphs != DiscordNullableBoolean.None)
            {
                guildSettings.TrackedRecords.IsReported = reportTriumphs is DiscordNullableBoolean.True;
            }

            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Updated report settings")
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("item-add", "Adds new item to tracking")]
    public async Task AddCollectibleToTrackingAsync(
       [Summary("item", "Item to add to tracking")][Autocomplete(typeof(DestinyExcludingCollectibleDefinitionAutocompleter))] uint itemHash)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, lang, out var collectibleDefinition))
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(itemHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedCollectibles.TrackedHashes.Add(itemHash);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            var (name, icon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, lang);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Added **{name}** to tracking")
                .WithThumbnailUrl(icon)
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("item-remove", "Removes item from tracking")]
    public async Task RemoveCollectibleFromTrackingAsync(
       [Summary("item", "Item to remove from tracking")][Autocomplete(typeof(DestinyDbCollectibleDefinitionAutocompleter))] uint itemHash)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, lang, out var collectibleDefinition))
            {
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(itemHash);
            }

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedCollectibles.TrackedHashes.Remove(itemHash);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            var (name, icon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, lang);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Removed **{name}** from tracking")
                .WithThumbnailUrl(icon)
                .Build());
        });
    }

    [SlashCommand("items-show", "Shows all tracked items")]
    public async Task ShowTrackedItemsAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            if (!guildSettings.TrackedCollectibles.TrackedHashes.Any())
            {
                return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                        "Tracked items",
                        $"Currently there's no tracked items")
                    .Build());
            }

            var lang = await _localizationService.GetGuildLocale(GuildId);

            var sb = new StringBuilder();

            foreach (var collectibleHash in guildSettings.TrackedCollectibles.TrackedHashes)
            {
                if (!client.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, lang, out var collectibleDefinition))
                {
                    sb.AppendLine($"> Unknown definition hash {collectibleHash}");
                }
                else
                {
                    var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, lang);
                    sb.AppendLine($"> {name}");
                }
            }

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    $"Tracked items: {guildSettings.TrackedCollectibles.TrackedHashes.Count}",
                    sb.ToString().LimitTo(4096))
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("triumph-add", "Adds new triumph to tracking")]
    public async Task AddRecordToTrackingAsync(
       [Summary("triumph", "Item to add to tracking")][Autocomplete(typeof(DestinyExcludingRecordDefinitionAutocompleter))] uint recordHash)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyRecordDefinition>(recordHash, lang, out var recordDefinition))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedRecords.TrackedHashes.Add(recordHash);

            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Added **{recordDefinition.DisplayProperties.Name}** to tracking")
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath)
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("triumph-remove", "Removes triumph from tracking")]
    public async Task RemoveRecordFromTrackingAsync(
       [Summary("triumph", "Triumph to remove from tracking")][Autocomplete(typeof(DestinyDbRecordDefinitionAutocompleter))] uint recordHash)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyRecordDefinition>(recordHash, lang, out var recordDefinition))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedRecords.TrackedHashes.Remove(recordHash);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Removed **{recordDefinition.DisplayProperties.Name}** from tracking")
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath)
                .Build());
        });
    }

    [SlashCommand("triumphs-show", "Shows all tracked triumphs")]
    public async Task ShowTrackedRecordsAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            if (!guildSettings.TrackedRecords.TrackedHashes.Any())
            {
                return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                        "Tracked triumphs",
                        $"Currently there's no tracked triumphs")
                    .Build());
            }

            var lang = await _localizationService.GetGuildLocale(GuildId);

            var sb = new StringBuilder();

            foreach (var collectibleHash in guildSettings.TrackedRecords.TrackedHashes)
            {
                if (!client.TryGetDefinition<DestinyRecordDefinition>(collectibleHash, lang, out var recordDefinition))
                {
                    sb.AppendLine($"> Unknown definition hash {collectibleHash}");
                }
                else
                {
                    sb.AppendLine($"> {recordDefinition.DisplayProperties.Name}");
                }
            }

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    $"Tracked triumphs: {guildSettings.TrackedRecords.TrackedHashes.Count}",
                    sb.ToString().LimitTo(4096))
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("system-reports", "Sets system report settings")]
    public async Task SetSystemReportSettings(
        [Summary(description: "Whether to report system notifications")] DiscordNullableBoolean enableSystemReports = DiscordNullableBoolean.None,
        [Summary(description: "Channel to report them to")] IChannel? channel = null)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var currentGuildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (currentGuildSettings is null)
                return GuildSettingsNotFound();

            var anyChanges = false;
            if (enableSystemReports != DiscordNullableBoolean.None)
            {
                var newValue = enableSystemReports is DiscordNullableBoolean.True;
                if (currentGuildSettings.SystemReportsEnabled != newValue)
                {
                    currentGuildSettings.SystemReportsEnabled = newValue;
                    anyChanges = true;
                }
            }

            if (channel is not null)
            {
                var channelId = channel.Id;

                if (currentGuildSettings.SystemReportsOverrideChannel != channelId)
                {
                    currentGuildSettings.SystemReportsOverrideChannel = channelId;
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                await _destinyDb.UpsertGuildSettingsAsync(currentGuildSettings);
            }

            var embed = _embedBuilderService
                .CreateSimpleResponseEmbed("Success", "Guild settings updated")
                .Build();

            return Success(embed);
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("item-add-set", "Adds all items from set to tracking")]
    public async Task AddAllCollectiblesFromNode(
        [Summary("presentation-node")][Autocomplete(typeof(DestinyPresentationNodeWithCollectiblesAutocompleter))] string presentationNodeHashString)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(presentationNodeHashString, out var presentationNodeHash))
                return Error($"Failed to parse presentation node hash");

            var client = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyPresentationNodeDefinition>(presentationNodeHash, lang, out var presentationNodeDefinition))
                return DestinyDefinitionNotFound<DestinyPresentationNodeDefinition>(presentationNodeHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            foreach (var collectibleHash in presentationNodeDefinition.Children.Collectibles)
            {
                guildSettings.TrackedCollectibles.TrackedHashes.Add(collectibleHash.Collectible.Hash.GetValueOrDefault());
            }

            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            var embed = _embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Added **{presentationNodeDefinition.DisplayProperties.Name}** ({presentationNodeDefinition.Children.Collectibles.Count} item(s)) to tracking")
                .WithThumbnailUrl(presentationNodeDefinition.DisplayProperties.Icon.AbsolutePath)
                .Build();

            return Success(embed);
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("item-remove-set", "Adds all items from set to tracking")]
    public async Task RemoveAllCollectiblesFromNode(
        [Summary("presentation-node")][Autocomplete(typeof(DestinyPresentationNodeWithCollectiblesAutocompleter))] string presentationNodeHashString)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            if (!uint.TryParse(presentationNodeHashString, out var presentationNodeHash))
                return Error($"Failed to parse presentation node hash");

            var client = await _bungieClientProvider.GetClientAsync();
            var lang = await _localizationService.GetGuildLocale(GuildId);

            if (!client.TryGetDefinition<DestinyPresentationNodeDefinition>(presentationNodeHash, lang, out var presentationNodeDefinition))
                return DestinyDefinitionNotFound<DestinyPresentationNodeDefinition>(presentationNodeHash);

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            foreach (var collectibleHash in presentationNodeDefinition.Children.Collectibles)
            {
                guildSettings.TrackedCollectibles.TrackedHashes.Remove(collectibleHash.Collectible.Hash.GetValueOrDefault());
            }

            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            var embed = _embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Removed **{presentationNodeDefinition.DisplayProperties.Name}** ({presentationNodeDefinition.Children.Collectibles.Count} item(s)) from tracking")
                .WithThumbnailUrl(presentationNodeDefinition.DisplayProperties.Icon.AbsolutePath)
                .Build();

            return Success(embed);
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("set-destiny-locale", "Sets locale user by destiny definitions, guild scoped")]
    public async Task SetGuildDestinyLocale(
        [Summary("locale", "Locale to use")] DiscordDestinyLanguageEnum locale)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await Context.Interaction.DeferAsync();

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.DestinyManifestLocale = locale;

            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            await _bungieClientProvider.ReloadClient();

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Changed locale language to {locale}")
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("rescan-clan", "Queues up clan for rescan, might not work if clan is already being scanned")]
    public async Task SetClanRescan(
        [Autocomplete(typeof(DestinyClanFromGuildAutocompleter)), Summary("Clan")] long clanToRescan)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await _destinyDb.SetClanRescanAsync(clanToRescan);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Queued clan for rescan: {clanToRescan}")
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("rescan-clans-all", "Queues up all clans for rescan, might not work if some clans are already being scanned")]
    public async Task SetClansAllRescan()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await _destinyDb.SetClanRescanForAllTrackedClansAsync();

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Queued up all clans for rescan")
                .Build());
        });
    }
}
