using Atheon.Models.Database.Administration;
using Atheon.Models.Database.Destiny.Links;
using Atheon.Models.Destiny;
using Atheon.Services.DiscordHandlers.Autocompleters;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.DiscordHandlers.Preconditions;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("settings", "Group of commands related to guild settings")]
public class SettingsCommandHandler : SlashCommandHandlerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IClanQueue _clanQueue;
    private readonly EmbedBuilderService _embedBuilderService;

    public SettingsCommandHandler(
        ILogger<SettingsCommandHandler> logger,
        IDestinyDb destinyDb,
        IClanQueue clanQueue,
        EmbedBuilderService embedBuilderService) : base(logger)
    {
        _destinyDb = destinyDb;
        _clanQueue = clanQueue;
        _embedBuilderService = embedBuilderService;
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-add", "Adds new clan to guild")]
    public async Task AddClanToGuildAsync(
        [Autocomplete(typeof(DestinyClanByIdAutocompleter)), Summary("Clan")] long clanId)
    {
        var settings = await _destinyDb.GetGuildSettingsAsync(GuildId);

        if (settings.Clans.Contains(clanId))
        {
            var embedTemplate = _embedBuilderService.CreateSimpleResponseEmbed("Add new clan failed", "Clan is already added to this discord guild");
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
        return;
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("clan-remove", "Removes selected clan from guild")]
    public async Task RemoveClanFromGuildAsync(
        [Autocomplete(typeof(DestinyClanFromGuildAutocompleter)), Summary("Clan")] long clanIdToRemove)
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
    }

    [AtheonBotAdminOrOwner]
    [SlashCommand("link-user", "Links discord user to destiny profile")]
    public async Task LinkUserAsync(
        [Autocomplete(typeof(SearchDestinyUserByNameAutocompleter))][Summary("User")] DestinyProfilePointer user,
        [Summary("link-to", "User to link to")] IUser? linkTo = null)
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
    }

    [RequireOwner]
    [SlashCommand("admin-add", "Links discord user to destiny profile")]
    public async Task AddServerAdminAsync(
        [Summary("user", "New server admin")] IUser user)
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
    }

    [RequireOwner]
    [SlashCommand("admin-remove", "Links discord user to destiny profile")]
    public async Task RemoveServerAdminAsync(
        [Summary("user", "Server admin to remove")] IUser user)
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
    }
}
