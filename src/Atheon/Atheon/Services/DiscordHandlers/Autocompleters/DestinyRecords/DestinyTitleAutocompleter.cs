using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.HashReferences;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;

public class DestinyTitleAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly ILocalizationService _localizationService;

    public DestinyTitleAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        DestinyDefinitionDataService destinyDefinitionDataService,
        ILocalizationService localizationService)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
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

        var client = await _bungieClientProvider.GetClientAsync();

        var titles = await _destinyDefinitionDataService.GetAllTitleDefinitionsAsync(lang);

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
