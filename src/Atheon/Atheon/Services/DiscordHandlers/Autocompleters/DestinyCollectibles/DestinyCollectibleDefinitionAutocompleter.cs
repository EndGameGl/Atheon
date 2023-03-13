using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using System.Linq;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;

public class DestinyCollectibleDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IMemoryCache _memoryCache;
    private readonly IDestinyDb _destinyDb;

    public DestinyCollectibleDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        DestinyDefinitionDataService destinyDefinitionDataService,
        IMemoryCache memoryCache,
        IDestinyDb destinyDb)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
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
        try
        {
            var lang = await _memoryCache.GetOrAddAsync(
                $"guild_lang_{context.Guild.Id}",
                async () => (await _destinyDb.GetGuildLanguageAsync(context.Guild.Id)).ConvertToBungieLocale(),
                TimeSpan.FromSeconds(15),
                Caching.CacheExpirationType.Absolute);

            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

            var searchResults = client
                .Repository
                .GetAll<DestinyCollectibleDefinition>(lang)
                .Where(x =>
                {
                    if (x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }

                    var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(x, lang);

                    return name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase);
                })
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(GetCollectibleDisplayName(x, lang), x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to form collectibles for query");
            return AutocompletionResult.FromSuccess();
        }
    }

    private string GetCollectibleDisplayName(DestinyCollectibleDefinition destinyCollectible, BungieLocales locale)
    {
        var (name, _) = _destinyDefinitionDataService.GetCollectibleDisplayProperties(destinyCollectible, locale);

        if (destinyCollectible.Item.HasValidHash)
        {
            return new string($"{name} ({destinyCollectible.Item.Select(x => x.ItemTypeAndTierDisplayName, locale)})".Take(100).ToArray());
        }

        return new string($"{name}".Take(100).ToArray());
    }
}
