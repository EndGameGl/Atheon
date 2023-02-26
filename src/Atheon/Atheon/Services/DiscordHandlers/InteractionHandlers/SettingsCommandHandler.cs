using Atheon.Services.DiscordHandlers.Autocompleters;
using Atheon.Services.DiscordHandlers.EmbedBuilders;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("settings", "Group of commands related to guild settings")]
public class SettingsCommandHandler : SlashCommandHandlerBase
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly IDestinyDb _destinyDb;
    private readonly IClanQueue _clanQueue;

    public SettingsCommandHandler(
        ILogger<SettingsCommandHandler> logger,
        IBungieClientProvider bungieClientProvider,
        IDestinyDb destinyDb,
        IClanQueue clanQueue) : base(logger)
    {
        _bungieClientProvider = bungieClientProvider;
        _destinyDb = destinyDb;
        _clanQueue = clanQueue;
    }

    [SlashCommand("clan", "Adds new clan to guild")]
    public async Task AddClanToGuildAsync(
        [Autocomplete(typeof(DestinyClanByIdAutocompleter)), Summary("")] long clanId)
    {
        var settings = await _destinyDb.GetGuildSettingsAsync(GuildId);

        if (settings.Clans.Contains(clanId))
        {
            var embedTemplate = Embeds.GetGenericEmbed("Add new clan failed", Discord.Color.Red, "Clan is already added to this discord guild");
            await Context.Interaction.RespondAsync(embed: embedTemplate.Build());
            return;
        }

        var existingClan = await _destinyDb.GetClanModelAsync(clanId);

        if (existingClan is not null)
        {
            settings.Clans.Add(clanId);
            await _destinyDb.UpsertGuildSettingsAsync(settings);
            var embedTemplate = Embeds.GetGenericEmbed("Add new clan success", Discord.Color.Green, "Added clan to this guild");
            await Context.Interaction.RespondAsync(embed: embedTemplate.Build());
            return;
        }

        _clanQueue.EnqueueFirstTimeScan(clanId);
        settings.Clans.Add(clanId);
        await _destinyDb.UpsertGuildSettingsAsync(settings);
        await Context.Interaction.RespondAsync(embed:
            Embeds.GetGenericEmbed(
                "Add new clan success",
                Discord.Color.Green,
                "New clan will be ready when scan message pops up").Build());
        return;
    }
}
