using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;

public class DestinyExcludingRecordDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IGuildDb _guildDb;

    public DestinyExcludingRecordDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        ILocalizationService localizationService,
        IGuildDb guildDb)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
        _localizationService = localizationService;
        _guildDb = guildDb;
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
                .GetAll<DestinyRecordDefinition>(lang)
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Where(x => !settings.TrackedRecords.TrackedHashes.Contains(x.Hash))
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(x.DisplayProperties.Name, x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to form records for query");
            return AutocompletionResult.FromSuccess();
        }
    }
}
