using Atheon.Services.BungieApi;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;

public class DestinyLeaderboardValidRecordDefinitionAutocompleter : AutocompleteHandler
{
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;

    public DestinyLeaderboardValidRecordDefinitionAutocompleter(
        DestinyDefinitionDataService destinyDefinitionDataService)
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
                .LeaderboardValidRecords
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Take(20);

        var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(x.DisplayProperties.Name, x.Hash.ToString()));

        return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
    }
}
