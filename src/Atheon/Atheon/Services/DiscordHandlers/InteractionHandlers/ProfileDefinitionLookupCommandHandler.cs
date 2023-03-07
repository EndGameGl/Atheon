using Atheon.Models.Database.Destiny.Clans;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("lookup", "Set of commands to check user statuses")]
public class ProfileDefinitionLookupCommandHandler : SlashCommandHandlerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly EmbedBuilderService _embedBuilderService;

    public ProfileDefinitionLookupCommandHandler(
        ILogger<ProfileDefinitionLookupCommandHandler> logger,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider,
        EmbedBuilderService embedBuilderService) : base(logger, embedBuilderService)
    {
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
        _embedBuilderService = embedBuilderService;
    }

    [SlashCommand("item-check", "Checks who has items")]
    public async Task GetUsersWithItem(
        [Autocomplete(typeof(DestinyCollectibleDefinitionAutocompleter))][Summary(description: "Collectible")] string collectibleHash,
        [Summary(description: "Whether user has item or not")] bool hasItem,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var itemHash = uint.Parse(collectibleHash);
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetProfilesCollectibleStatusAsync(itemHash, hasItem, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var bungieClient = await _bungieClientProvider.GetClientAsync();

            bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, DotNetBungieAPI.Models.BungieLocales.EN, out var colDef);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"{users.Count} users {(hasItem ? "have" : "miss")} {colDef.DisplayProperties.Name}");

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

            await Context.Interaction.RespondAsync(
                embed: embedBuilder
                    .WithThumbnailUrl(colDef.DisplayProperties.Icon.AbsolutePath)
                    .Build(),
                ephemeral: hide);
        });
    }

    [SlashCommand("triumph-check", "Checks who completed triumph")]
    public async Task GetUsersWithRecord(
        [Autocomplete(typeof(DestinyRecordDefinitionAutocompleter))][Summary(description: "Record")] string recordHash,
        [Summary(description: "Whether user has completed triumph or not")] bool hasCompletedTriumph,
        [Summary(description: "Whether to hide this message")] bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var itemHash = uint.Parse(recordHash);
            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var users = await _destinyDb.GetProfilesRecordStatusAsync(itemHash, hasCompletedTriumph, guildSettings.Clans.ToArray());
            var clanIds = users.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);
            var bungieClient = await _bungieClientProvider.GetClientAsync();

            bungieClient.TryGetDefinition<DestinyRecordDefinition>(itemHash, BungieLocales.EN, out var recordDef);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"{users.Count} users have{(hasCompletedTriumph ? " " : " not ")}completed {recordDef.DisplayProperties.Name}");

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

            await Context.Interaction.RespondAsync(
                embed: embedBuilder
                    .WithThumbnailUrl(recordDef.DisplayProperties.Icon.AbsolutePath)
                    .Build(),
                ephemeral: hide);
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
        uint collectibleHash,
        [Summary(description: "Whether to hide this message")]
        bool hide = false)
    {
        await ExecuteAndHanldeErrors(async () =>
        {
            var client = await _bungieClientProvider.GetClientAsync();
            if (!client.TryGetDefinition<DestinyCollectibleDefinition>(collectibleHash, BungieLocales.EN, out var collectibleDefinition))
            {
                var embed = _embedBuilderService.CreateSimpleResponseEmbed("Failure", "Failed to get definition", Color.Red).Build();
                await Context.Interaction.RespondAsync(embed: embed);
                return;
            }

            var guildSettings = await _destinyDb.GetGuildSettingsAsync(GuildId);
            var drystreaks = await _destinyDb.GetProfileDrystreaksAsync(collectibleHash, guildSettings.Clans.ToArray());
            var clanIds = drystreaks.Select(x => x.ClanId).Distinct().ToArray();
            var clanReferences = await _destinyDb.GetClanReferencesFromIdsAsync(clanIds);

            var embedBuilder = _embedBuilderService
                .GetTemplateEmbed()
                .WithTitle($"Users who don't have {collectibleDefinition.DisplayProperties.Name}");

            for (int j = 0; j < clanReferences.Count; j++)
            {
                var reference = clanReferences[j];
                var sb = new StringBuilder();
                sb.Append("```");
                var usersOfClan = drystreaks.Where(x => x.ClanId == reference.Id).ToList();

                for (int i = 0; i < usersOfClan.Count; i++)
                {
                    var user = usersOfClan[i];
                    if (user.Name is "#")
                        continue;
                    var userDisplayString = $"{user.Name}: {user.Value}\n";
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

            await Context.Interaction.RespondAsync(
                embed: embedBuilder
                    .WithThumbnailUrl(collectibleDefinition.DisplayProperties.Icon.AbsolutePath)
                    .Build(),
                ephemeral: hide);
        });
    }
}
