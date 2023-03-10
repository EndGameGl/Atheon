using Atheon.Services.BungieApi;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyPresentationNodes;

public class DestinyPresentationNodeWithCollectiblesAutocompleter : AutocompleteHandler
{
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public DestinyPresentationNodeWithCollectiblesAutocompleter(DestinyDefinitionDataService destinyDefinitionDataService)
    {
        _destinyDefinitionDataService = destinyDefinitionDataService;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, 
        IAutocompleteInteraction autocompleteInteraction, 
        IParameterInfo parameter, 
        IServiceProvider services)
    {
        var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;
        var searchResults = _destinyDefinitionDataService
            .FindNodes(searchEntry)
            .Take(20)
            .Select(x => new AutocompleteResult(x.NodeFulleName, x.Hash.ToString()));

        return !searchResults.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(searchResults);
    }
}
