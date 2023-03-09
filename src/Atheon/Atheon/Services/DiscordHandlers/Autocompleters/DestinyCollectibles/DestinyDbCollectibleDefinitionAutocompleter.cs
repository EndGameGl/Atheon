using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;

public class DestinyDbCollectibleDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly IDestinyDb _destinyDb;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public DestinyDbCollectibleDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        IDestinyDb destinyDb,
        DestinyDefinitionDataService destinyDefinitionDataService)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
        _destinyDb = destinyDb;
        _destinyDefinitionDataService = destinyDefinitionDataService;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        try
        {
            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;
            var settings = await _destinyDb.GetGuildSettingsAsync(context.Guild.Id);

            if (settings is null)
                return AutocompletionResult.FromSuccess();

            var searchResults = client
                .Repository
                .GetAll<DestinyCollectibleDefinition>()
                .Where(x => settings.TrackedCollectibles.TrackedHashes.Contains(x.Hash))
                .Where(x =>
                {
                    if (x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }

                    var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(x);

                    return name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase);
                })
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(GetCollectibleDisplayName(x), x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to form collectibles for query");
            return AutocompletionResult.FromSuccess();
        }
    }

    private string GetCollectibleDisplayName(DestinyCollectibleDefinition destinyCollectible)
    {
        var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(destinyCollectible);

        if (destinyCollectible.Item.HasValidHash)
        {
            return new string($"{name} ({destinyCollectible.Item.Select(x => x.ItemTypeAndTierDisplayName)})".Take(100).ToArray());
        }

        return new string($"{name}".Take(100).ToArray());
    }
}
