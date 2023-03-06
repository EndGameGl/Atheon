using Atheon.Models.Database.Administration;
using Atheon.Models.Database.Destiny.Links;
using Atheon.Models.Destiny;
using Atheon.Services.DiscordHandlers.Autocompleters;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.DiscordHandlers.Preconditions;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using System.Text;
using System.Threading.Channels;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("settings", "Group of commands related to guild settings")]
public class SettingsCommandHandler : SlashCommandHandlerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IClanQueue _clanQueue;
    private readonly EmbedBuilderService _embedBuilderService;
    private readonly IBungieClientProvider _bungieClientProvider;

    public SettingsCommandHandler(
        ILogger<SettingsCommandHandler> logger,
        IDestinyDb destinyDb,
        IClanQueue clanQueue,
        EmbedBuilderService embedBuilderService,
        IBungieClientProvider bungieClientProvider) : base(logger, embedBuilderService)
    {
        _destinyDb = destinyDb;
        _clanQueue = clanQueue;
        _embedBuilderService = embedBuilderService;
        _bungieClientProvider = bungieClientProvider;
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-add", "Adds new clan to guild")]
    public async Task AddClanToGuildAsync(
        [Autocomplete(typeof(DestinyClanByIdAutocompleter)), Summary("Clan")] long clanId)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var settings = await _destinyDb.GetGuildSettingsAsync(GuildId);

            if (settings.Clans.Contains(clanId))
            {
                var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed("Add new clan failed", "Clan is already added to this discord guild");
                var existingGuildClan = await _destinyDb.GetClanModelAsync(clanId)!;
                existingGuildClan.IsTracking = true;
                await _destinyDb.UpsertClanModelAsync(existingGuildClan);
                await Context.Interaction.RespondAsync(embed: embedTemplate.Build());
                return;
            }

            var existingClan = await _destinyDb.GetClanModelAsync(clanId);

            if (existingClan is not null)
            {
                settings.Clans.Add(clanId);
                await _destinyDb.UpsertGuildSettingsAsync(settings);
                var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed("Add new clan success", "Added clan to this guild");
                await Context.Interaction.RespondAsync(embed: embedTemplate.Build());
                return;
            }

            _clanQueue.EnqueueFirstTimeScan(clanId);
            settings.Clans.Add(clanId);
            await _destinyDb.UpsertGuildSettingsAsync(settings);
            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
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
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(Context.Guild.Id);
            if (guildSettings is null)
                return;

            guildSettings.Clans.Remove(clanIdToRemove);
            var clanModel = await _destinyDb.GetClanModelAsync(clanIdToRemove);
            if (clanModel is null)
                return;

            clanModel.IsTracking = false;

            await _destinyDb.UpsertClanModelAsync(clanModel);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            await Context.Interaction.RespondAsync(embed: _embedBuilderService.CreateSimpleResponseEmbed($"Removed clan {clanIdToRemove}", "Success").Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("link-user", "Links discord user to destiny profile")]
    public async Task LinkUserAsync(
        [Autocomplete(typeof(SearchDestinyUserByNameAutocompleter))][Summary("User")] DestinyProfilePointer user,
        [Summary("link-to", "User to link to")] IUser? linkTo = null)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var userToLinkTo = linkTo is null ? Context.User : linkTo;

            var link = new DiscordToDestinyProfileLink()
            {
                DiscordUserId = userToLinkTo.Id,
                DestinyMembershipId = user.MembershipId,
                BungieMembershipType = user.MembershipType
            };

            await _destinyDb.UpsertProfileLinkAsync(link);

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    "User updated",
                    $"{userToLinkTo.Mention} is now linked to {user.MembershipId}").Build());
        });
    }

    [RequireOwner]
    [SlashCommand("admin-add", "Links discord user to destiny profile")]
    public async Task AddServerAdminAsync(
        [Summary("user", "New server admin")] IUser user)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            await _destinyDb.AddServerAdministratorAsync(new ServerBotAdministrator()
            {
                DiscordGuildId = Context.Guild.Id,
                DiscordUserId = user.Id
            });

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    "Admin added",
                    $"{user.Mention} is now bot admin").Build());
        });
    }

    [RequireOwner]
    [SlashCommand("admin-remove", "Links discord user to destiny profile")]
    public async Task RemoveServerAdminAsync(
        [Summary("user", "Server admin to remove")] IUser user)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            await _destinyDb.RemoveServerAdministratorAsync(new ServerBotAdministrator()
            {
                DiscordGuildId = Context.Guild.Id,
                DiscordUserId = user.Id
            });

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    "Admin removed",
                    $"{user.Mention} is not bot admin anymore").Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("set-report-channel", "Sets report channel")]
    public async Task SetReportChannelAsync(
        [Summary("channel", "Channel to send reports to")][ChannelTypes(ChannelType.Text)] IChannel channel)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            guildSettings.DefaultReportChannel = channel.Id;
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Default report channel is now <#{channel.Id}>")
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("item-add", "Adds new item to tracking")]
    public async Task AddCollectibleToTrackingAsync(
       [Summary("item", "Item to add to tracking")][Autocomplete(typeof(DestinyExcludingCollectibleDefinitionAutocompleter))] uint itemHash)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, BungieLocales.EN, out var collectibleDefinition))
            {
                await Context.Interaction.RespondAsync(embed:
                    _embedBuilderService.CreateSimpleResponseEmbed(
                        "Failure",
                        $"Couldn't find definition in database",
                        Color.Red)
                    .Build());
                return;
            }

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            guildSettings.TrackedCollectibles.TrackedHashes.Add(itemHash);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Added **{collectibleDefinition.DisplayProperties.Name}** to tracking")
                .WithThumbnailUrl(collectibleDefinition.DisplayProperties.Icon.AbsolutePath)
                .Build());
        });
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("item-remove", "Removes item from tracking")]
    public async Task RemoveCollectibleFromTrackingAsync(
       [Summary("item", "Item to remove from tracking")][Autocomplete(typeof(DestinyDbCollectibleDefinitionAutocompleter))] uint itemHash)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, BungieLocales.EN, out var collectibleDefinition))
            {
                await Context.Interaction.RespondAsync(embed:
                    _embedBuilderService.CreateSimpleResponseEmbed(
                        "Failure",
                        $"Couldn't find definition in database",
                        Color.Red)
                    .Build());
                return;
            }

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            guildSettings.TrackedCollectibles.TrackedHashes.Remove(itemHash);
            await _destinyDb.UpsertGuildSettingsAsync(guildSettings);

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    "Success",
                    $"Removed **{collectibleDefinition.DisplayProperties.Name}** from tracking")
                .WithThumbnailUrl(collectibleDefinition.DisplayProperties.Icon.AbsolutePath)
                .Build());
        });
    }

    [SlashCommand("items-show", "Shows all tracked items")]
    public async Task ShowTrackedItemsAsync()
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);

            if (!guildSettings.TrackedCollectibles.TrackedHashes.Any())
            {
                await Context.Interaction.RespondAsync(embed:
                    _embedBuilderService.CreateSimpleResponseEmbed(
                        "Tracked items",
                        $"Currently there's no tracked items")
                    .Build());
                return;
            }

            var sb = new StringBuilder();

            foreach (var collectibleHash in guildSettings.TrackedCollectibles.TrackedHashes)
            {
                if (!client.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, BungieLocales.EN, out var collectibleDefinition))
                {
                    sb.AppendLine($"> Unknown definition hash {collectibleHash}");
                }
                else
                {
                    sb.AppendLine($"> {collectibleDefinition.DisplayProperties.Name}");
                }
            }

            await Context.Interaction.RespondAsync(embed:
                _embedBuilderService.CreateSimpleResponseEmbed(
                    $"Tracked items: {guildSettings.TrackedCollectibles.TrackedHashes.Count}",
                    sb.ToString())
                .Build());
        });
    }
}
