using Atheon.Services.DiscordHandlers.Autocompleters;
using Atheon.Services.DiscordHandlers.InteractionHandlers.Base;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using System.Text;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers;

[Group("lookup", "Set of commands to check user statuses")]
public class ProfileDefinitionLookupCommandHandler : SlashCommandHandlerBase
{
    private readonly IDestinyDb _destinyDb;
    private readonly IBungieClientProvider _bungieClientProvider;

    public ProfileDefinitionLookupCommandHandler(
        ILogger<ProfileDefinitionLookupCommandHandler> logger,
        IDestinyDb destinyDb,
        IBungieClientProvider bungieClientProvider) : base(logger)
    {
        _destinyDb = destinyDb;
        _bungieClientProvider = bungieClientProvider;
    }

    [SlashCommand("item-check", "Checks who has items")]
    public async Task GetUsersWithItem(
        [Autocomplete(typeof(DestinyCollectibleDefinitionAutocompleter))][Summary(description: "Collectible")] string collectibleHash,
        [Summary(description: "Whether user has item or not")] bool hasItem)
    {
        var itemHash = uint.Parse(collectibleHash);
        var users = await _destinyDb.GetProfilesCollectibleStatusAsync(itemHash, hasItem);

        var bungieClient = await _bungieClientProvider.GetClientAsync();

        bungieClient.TryGetDefinition<DestinyCollectibleDefinition>(itemHash, DotNetBungieAPI.Models.BungieLocales.EN, out var colDef);

        var sb = new StringBuilder();
        foreach (var user in users)
        {
            var userDisplayString = $"> {user.Name}\n";
            if ((sb.Length + userDisplayString.Length) <= 2048)
            {
                sb.Append(userDisplayString);
            }
            else
            {
                break;
            }
        }

        await Context.Interaction.RespondAsync(
            embed: EmbedBuilders.Embeds.GetGenericEmbed(
                    $"{users.Count} users {(hasItem ? "have" : "miss")} {colDef.DisplayProperties.Name}",
                    Color.Green,
                    description: sb.ToString())
                .WithThumbnailUrl(colDef.DisplayProperties.Icon.AbsolutePath)
                .Build(),
            ephemeral: true);
    }

    [SlashCommand("triumph-check", "Checks who completed triumph")]
    public async Task GetUsersWithRecord(
        [Autocomplete(typeof(DestinyRecordDefinitionAutocompleter))][Summary(description: "Record")] string recordHash,
        [Summary(description: "Whether user has completed triumph or not")] bool hasCompletedTriumph)
    {
        var itemHash = uint.Parse(recordHash);
        var users = await _destinyDb.GetProfilesRecordStatusAsync(itemHash, hasCompletedTriumph);

        var bungieClient = await _bungieClientProvider.GetClientAsync();

        bungieClient.TryGetDefinition<DestinyRecordDefinition>(itemHash, DotNetBungieAPI.Models.BungieLocales.EN, out var recordDef);

        var sb = new StringBuilder();
        foreach (var user in users)
        {
            var userDisplayString = $"> {user.Name}\n";
            if ((sb.Length + userDisplayString.Length) <= 2048)
            {
                sb.Append(userDisplayString);
            }
            else
            {
                break;
            }
        }

        await Context.Interaction.RespondAsync(
            embed: EmbedBuilders.Embeds.GetGenericEmbed(
                    $"{users.Count} users have{(hasCompletedTriumph ? " " : " not ")}completed {recordDef.DisplayProperties.Name}",
                    Color.Green,
                    description: sb.ToString())
                .WithThumbnailUrl(recordDef.DisplayProperties.Icon.AbsolutePath)
                .Build(),
            ephemeral: true);
    }
}
