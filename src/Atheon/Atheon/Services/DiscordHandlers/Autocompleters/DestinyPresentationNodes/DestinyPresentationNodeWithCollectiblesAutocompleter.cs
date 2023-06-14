using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyPresentationNodes;

public class DestinyPresentationNodeWithCollectiblesAutocompleter : AutocompleteHandler
{
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly ILocalizationService _localizationService;

    public DestinyPresentationNodeWithCollectiblesAutocompleter(
        DestinyDefinitionDataService destinyDefinitionDataService,
        ILocalizationService localizationService)
    {
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _localizationService = localizationService;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var lang = await _localizationService.GetGuildLocaleCachedAsync(context.Guild.Id);

        var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;
        var searchResults = _destinyDefinitionDataService
            .FindNodes(searchEntry, lang)
            .Take(20)
            .Select(x => new AutocompleteResult(x.NodeFulleName, x.Hash.ToString()));

        return !searchResults.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(searchResults);
    }
}
