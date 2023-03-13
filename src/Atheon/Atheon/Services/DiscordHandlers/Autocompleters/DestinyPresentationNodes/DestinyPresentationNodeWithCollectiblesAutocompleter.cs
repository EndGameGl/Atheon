using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyPresentationNodes;

public class DestinyPresentationNodeWithCollectiblesAutocompleter : AutocompleteHandler
{
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IMemoryCache _memoryCache;
    private readonly IDestinyDb _destinyDb;

    public DestinyPresentationNodeWithCollectiblesAutocompleter(
        DestinyDefinitionDataService destinyDefinitionDataService,
        IDestinyDb destinyDb,
        IMemoryCache memoryCache)
    {
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _memoryCache = memoryCache;
        _destinyDb = destinyDb;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var lang = await _memoryCache.GetOrAddAsync(
                $"guild_lang_{context.Guild.Id}",
                async () => (await _destinyDb.GetGuildLanguageAsync(context.Guild.Id)).ConvertToBungieLocale(),
                TimeSpan.FromSeconds(15),
                Caching.CacheExpirationType.Absolute);

        var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;
        var searchResults = _destinyDefinitionDataService
            .FindNodes(searchEntry, lang)
            .Take(20)
            .Select(x => new AutocompleteResult(x.NodeFulleName, x.Hash.ToString()));

        return !searchResults.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(searchResults);
    }
}
