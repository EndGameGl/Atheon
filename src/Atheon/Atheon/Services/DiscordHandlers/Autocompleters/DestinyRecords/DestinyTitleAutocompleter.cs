using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using DotNetBungieAPI.HashReferences;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;

public class DestinyTitleAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public DestinyTitleAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        DestinyDefinitionDataService destinyDefinitionDataService)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
        _destinyDefinitionDataService = destinyDefinitionDataService;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

        var client = await _bungieClientProvider.GetClientAsync();

        var titles = await _destinyDefinitionDataService.GetAllTitleDefinitionsAsync();

        var searchResults = titles
            .Where(x =>
                !string.IsNullOrEmpty(x.DisplayProperties.Name) &&
                (x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase) ||
                x.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine].Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase)))
            .Take(20);

        var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult($"{x.TitleInfo.TitlesByGenderHash[DefinitionHashes.Genders.Masculine]} ({x.DisplayProperties.Name})", x.Hash.ToString()));

        return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
    }
}
