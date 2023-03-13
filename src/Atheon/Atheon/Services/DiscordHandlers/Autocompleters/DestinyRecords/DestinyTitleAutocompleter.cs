using Atheon.Extensions;
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
    private readonly IDestinyDb _destinyDb;
    private readonly IMemoryCache _memoryCache;

    public DestinyTitleAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        DestinyDefinitionDataService destinyDefinitionDataService,
        IDestinyDb destinyDb,
        IMemoryCache memoryCache)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _destinyDb = destinyDb;
        _memoryCache = memoryCache;
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
