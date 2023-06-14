using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.BungieApi;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;

public class DestinyDbCollectibleDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly IGuildDb _guildDb;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILocalizationService _localizationService;

    public DestinyDbCollectibleDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        IGuildDb guildDb,
        DestinyDefinitionDataService destinyDefinitionDataService,
        IMemoryCache memoryCache,
        ILocalizationService localizationService)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
        _guildDb = guildDb;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _memoryCache = memoryCache;
        _localizationService = localizationService;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        try
        {
            var lang = await _localizationService.GetGuildLocaleCachedAsync(context.Guild.Id);

            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;
            var settings = await _guildDb.GetGuildSettingsAsync(context.Guild.Id);

            if (settings is null)
                return AutocompletionResult.FromSuccess();

            var searchResults = client
                .Repository
                .GetAll<DestinyCollectibleDefinition>(lang)
                .Where(x => settings.TrackedCollectibles.TrackedHashes.Contains(x.Hash))
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
            return new string($"{name} ({destinyCollectible.Item.Select(x => x.ItemTypeAndTierDisplayName, locale: locale)})".Take(100).ToArray());
        }

        return new string($"{name}".Take(100).ToArray());
    }
}
