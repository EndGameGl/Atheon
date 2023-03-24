using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;

public class DestinyLeaderboardValidRecordDefinitionAutocompleter : AutocompleteHandler
{
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IDestinyDb _destinyDb;
    private readonly IMemoryCache _memoryCache;

    public DestinyLeaderboardValidRecordDefinitionAutocompleter(
        DestinyDefinitionDataService destinyDefinitionDataService,
        IDestinyDb destinyDb,
        IMemoryCache memoryCache)
    {
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

        var searchResults = _destinyDefinitionDataService
                .LeaderboardValidRecords[lang]
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Take(20);

        var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(x.DisplayProperties.Name, x.Hash.ToString()));

        return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
    }
}
