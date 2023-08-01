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
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.PresentationNodes;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("settings", "Group of commands related to guild settings")]
public class SettingsCommandHandler : LocalizedSlashCommandHandler
{
    private readonly IDestinyDb _destinyDb;
    private readonly IGuildDb _guildDb;
    private readonly IClanQueue _clanQueue;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IServerAdminstrationDb _serverAdminstrationDb;

    public SettingsCommandHandler(
        ILogger<SettingsCommandHandler> logger,
        IDestinyDb destinyDb,
        IGuildDb guildDb,
        IClanQueue clanQueue,
        EmbedBuilderService embedBuilderService,
        IBungieClientProvider bungieClientProvider,
        DestinyDefinitionDataService destinyDefinitionDataService,
        ILocalizationService localizationService,
        IServerAdminstrationDb serverAdminstrationDb) : base(localizationService, logger, embedBuilderService)
    {
        _destinyDb = destinyDb;
        _guildDb = guildDb;
        _clanQueue = clanQueue;
        _embedBuilderService = embedBuilderService;
        _bungieClientProvider = bungieClientProvider;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _serverAdminstrationDb = serverAdminstrationDb;
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-add", "Adds new clan to guild")]
    public async Task AddClanToGuildAsync(
        [Autocomplete(typeof(DestinyClanByIdAutocompleter)), Summary("clan", "Clan ID to add")] long clanId)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var settings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (settings is null)
                return GuildSettingsNotFound();

            if (settings.Clans.Contains(clanId))
            {
                var existingGuildClan = await _destinyDb.GetClanModelAsync(clanId);
                if (existingGuildClan is null)
                    return Error(FormatText("ClanNotFoundError", () => "Clan {0} not found in database", clanId));

                existingGuildClan.IsTracking = true;
                await _destinyDb.UpsertClanModelAsync(existingGuildClan);

                var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed(
                    Text("AddClanFailedTitle", () => "Add new clan failed"),
                    Text("AddClanFailedDescription", () => "Clan is already added to this discord guild"));
                return Success(embedTemplate);
            }

            var existingClan = await _destinyDb.GetClanModelAsync(clanId);

            if (existingClan is not null)
            {
                settings.Clans.Add(clanId);
                await _guildDb.UpsertGuildSettingsAsync(settings);
                var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed(
                    Text("AddClanSuccessTitle", () => "Add new clan success"),
                    Text("AddClanSuccessDescription", () => "Added clan to this guild"));
                return Success(embedTemplate);
            }

            _clanQueue.EnqueueFirstTimeScan(clanId);
            settings.Clans.Add(clanId);
            await _guildDb.UpsertGuildSettingsAsync(settings);
            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("AddClanSuccessTitle", () => "Add new clan success"),
                    Text("AddClanSuccessResponseNew", () => "New clan will be ready when scan message pops up")));
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-remove", "Removes selected clan from guild")]
    public async Task RemoveClanFromGuildAsync(
        [Autocomplete(typeof(DestinyClanFromGuildAutocompleter)), Summary("clan", "Clan to remove")] long clanIdToRemove)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.Clans.Remove(clanIdToRemove);
            var clanModel = await _destinyDb.GetClanModelAsync(clanIdToRemove);
            if (clanModel is null)
                return Error(FormatText("ClanNotFoundError", () => "Clan {0} not found in database", clanIdToRemove));

            clanModel.IsTracking = false;

            await _destinyDb.UpsertClanModelAsync(clanModel);
            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                Text("RemoveClanSuccessTitle", () => "Clan removed"),
                FormatText("RemoveClanSuccessDescription", () => "Removed clan {0} from this server", clanIdToRemove)));
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("link-user", "Links discord user to destiny profile")]
    public async Task LinkUserAsync(
        [Autocomplete(typeof(SearchDestinyUserByNameAutocompleter))][Summary("user", "Destiny 2 user profile")] DestinyProfilePointer user,
        [Summary("link-to", "Discord user to link to")] IUser? linkTo = null)
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
                    Text("UserDataUpdatedTitle", () => "User updated"),
                    FormatText("UserDestinyMembershipLinked", () => "{0} is now linked to {1}", userToLinkTo.Mention, user.MembershipId)));
        });
    }

    [RequireOwner]
    [SlashCommand("admin-add", "Links discord user to destiny profile")]
    public async Task AddServerAdminAsync(
        [Summary("user", "New server admin")] IUser user)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await _serverAdminstrationDb.AddServerAdministratorAsync(new ServerBotAdministrator()
            {
                DiscordGuildId = Context.Guild.Id,
                DiscordUserId = user.Id
            });

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("AdminAddedSuccessTitle", () => "Admin added"),
                    FormatText("AdminAddedSuccessDescription", () => "{0} is now bot admin", user.Mention)));
        });
    }

    [RequireOwner]
    [SlashCommand("admin-remove", "Links discord user to destiny profile")]
    public async Task RemoveServerAdminAsync(
        [Summary("user", "Server admin to remove")] IUser user)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            await _serverAdminstrationDb.RemoveServerAdministratorAsync(new ServerBotAdministrator()
            {
                DiscordGuildId = GuildId,
                DiscordUserId = user.Id
            });

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                   Text("AdminRemovedSuccessTitle", () => "Admin removed"),
                   FormatText("AdminRemovedSuccessDescription", () => "{0} is not bot admin anymore", user.Mention)));
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("set-report-channel", "Sets report channel")]
    public async Task SetReportChannelAsync(
        [Summary("channel", "Channel to send reports to")][ChannelTypes(ChannelType.Text)] IChannel channel)
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.DefaultReportChannel = channel.Id;
            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText("DefaultReportChannelChangeResponse", () => "Default report channel is now <#{0}>", channel.Id)));
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
            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
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

            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    Text("ReportSettingsUpdatedDescription", () => "Updated report settings")));
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

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, out var collectibleDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(itemHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedCollectibles.TrackedHashes.Add(itemHash);
            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            var (name, icon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, GuildLocale);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText("ItemAddedToTrackingDescription", () => "Added **{0}** to tracking", name))
                .WithThumbnailUrl(icon));
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

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, out var collectibleDefinition, GuildLocale))
            {
                return DestinyDefinitionNotFound<DestinyCollectibleDefinition>(itemHash);
            }

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedCollectibles.TrackedHashes.Remove(itemHash);
            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            var (name, icon) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, GuildLocale);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText("ItemRemovedFromTrackingDescription", () => "Removed **{0}** from tracking", name))
                .WithThumbnailUrl(icon));
        });
    }

    [SlashCommand("items-show", "Shows all tracked items")]
    public async Task ShowTrackedItemsAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            if (!guildSettings.TrackedCollectibles.TrackedHashes.Any())
            {
                return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                        Text("TrackedItemsTitle", () => "Tracked items"),
                        Text("NoTrackedItemsDescription", () => "Currently there's no tracked items")));
            }

            var sb = new StringBuilder();

            foreach (var collectibleHash in guildSettings.TrackedCollectibles.TrackedHashes)
            {
                if (!client.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, out var collectibleDefinition, GuildLocale))
                {
                    sb.AppendLine($"> {FormatText("UnknownTrackedItem", () => "Unknown definition hash {0}", collectibleHash)}");
                }
                else
                {
                    var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(collectibleDefinition, GuildLocale);
                    sb.AppendLine($"> {name}");
                }
            }

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    FormatText("TrackedItemsCountTitle", () => "Tracked items: {0}", guildSettings.TrackedCollectibles.TrackedHashes.Count),
                    sb.ToString().LimitTo(4096)));
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

            if (!client.TryGetDefinition<DestinyRecordDefinition>(recordHash, out var recordDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedRecords.TrackedHashes.Add(recordHash);

            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText("ItemAddedToTrackingDescription", () => "Added **{0}** to tracking", recordDefinition.DisplayProperties.Name))
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath));
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

            if (!client.TryGetDefinition<DestinyRecordDefinition>(recordHash, out var recordDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyRecordDefinition>(recordHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.TrackedRecords.TrackedHashes.Remove(recordHash);
            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText("ItemRemovedFromTrackingDescription", () => "Removed **{0}** from tracking", recordDefinition.DisplayProperties.Name))
                .WithThumbnailUrl(recordDefinition.DisplayProperties.Icon.AbsolutePath));
        });
    }

    [SlashCommand("triumphs-show", "Shows all tracked triumphs")]
    public async Task ShowTrackedRecordsAsync()
    {
        await ExecuteAndHandleErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            if (!guildSettings.TrackedRecords.TrackedHashes.Any())
            {
                return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                        Text("TrackedTriumphsTitle", () => "Tracked triumphs"),
                        Text("NoTrackedTriumphsDescription", () => "Currently there's no tracked triumphs"))
                    .Build());
            }

            var sb = new StringBuilder();

            foreach (var recordHash in guildSettings.TrackedRecords.TrackedHashes)
            {
                if (!client.TryGetDefinition<DestinyRecordDefinition>(recordHash, out var recordDefinition, GuildLocale))
                {
                    sb.AppendLine($"> {FormatText("UnknownTrackedItem", () => "Unknown definition hash {0}", recordHash)}");
                }
                else
                {
                    sb.AppendLine($"> {recordDefinition.DisplayProperties.Name}");
                }
            }

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    FormatText("TrackedTriumphsCountTitle", () => "Tracked triumphs: {0}", guildSettings.TrackedRecords.TrackedHashes.Count),
                    sb.ToString().LimitTo(4096)));
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
            var currentGuildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
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
                await _guildDb.UpsertGuildSettingsAsync(currentGuildSettings);
            }

            var embed = _embedBuilderService
                .CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    Text("GuildSystemReportSettingsUpdated", () => "Guild system report settings updated"));

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
                return Error(FormatText("FailedToParsePresentationNodeHashError", () => "Failed to parse presentation node hash: {0}", presentationNodeHashString));

            var client = await _bungieClientProvider.GetClientAsync();

            if (!client.TryGetDefinition<DestinyPresentationNodeDefinition>(presentationNodeHash, out var presentationNodeDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyPresentationNodeDefinition>(presentationNodeHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            foreach (var collectibleHash in presentationNodeDefinition.Children.Collectibles)
            {
                guildSettings.TrackedCollectibles.TrackedHashes.Add(collectibleHash.Collectible.Hash.GetValueOrDefault());
            }

            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            var embed = _embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText(
                        "AddedAllItemsFromPresentationNodeDescription",
                        () => "Added **{0}** ({1} item(s)) to tracking",
                        presentationNodeDefinition.DisplayProperties.Name,
                        presentationNodeDefinition.Children.Collectibles.Count))
                .WithThumbnailUrl(presentationNodeDefinition.DisplayProperties.Icon.AbsolutePath);

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
                return Error(FormatText("FailedToParsePresentationNodeHashError", () => "Failed to parse presentation node hash: {0}", presentationNodeHashString));

            var client = await _bungieClientProvider.GetClientAsync();

            if (!client.TryGetDefinition<DestinyPresentationNodeDefinition>(presentationNodeHash, out var presentationNodeDefinition, GuildLocale))
                return DestinyDefinitionNotFound<DestinyPresentationNodeDefinition>(presentationNodeHash);

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            foreach (var collectibleHash in presentationNodeDefinition.Children.Collectibles)
            {
                guildSettings.TrackedCollectibles.TrackedHashes.Remove(collectibleHash.Collectible.Hash.GetValueOrDefault());
            }

            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            var embed = _embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText(
                        "RemovedAllItemsFromPresentationNodeDescription",
                        () => "Removed **{0}** ({1} item(s)) from tracking",
                        presentationNodeDefinition.DisplayProperties.Name,
                        presentationNodeDefinition.Children.Collectibles.Count))
                .WithThumbnailUrl(presentationNodeDefinition.DisplayProperties.Icon.AbsolutePath);

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

            var guildSettings = await _guildDb.GetGuildSettingsAsync(GuildId);
            if (guildSettings is null)
                return GuildSettingsNotFound();

            guildSettings.DestinyManifestLocale = locale;

            await _guildDb.UpsertGuildSettingsAsync(guildSettings);

            await _bungieClientProvider.ReloadClient();

            return Success(_embedBuilderService.CreateSimpleResponseEmbed(
                    Text("Success", () => "Success"),
                    FormatText("ChangedGuildLocale", () => "Changed locale language to {0}", locale)));
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
                    Text("Success", () => "Success"),
                    FormatText("QueuedClanForRescan", () => "Queued clan for rescan: {0}", clanToRescan)));
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
                    Text("Success", () => "Success"),
                    Text("QueuedAllClansForRescan", () => "Queued up all clans for rescan")));
        });
    }
}
