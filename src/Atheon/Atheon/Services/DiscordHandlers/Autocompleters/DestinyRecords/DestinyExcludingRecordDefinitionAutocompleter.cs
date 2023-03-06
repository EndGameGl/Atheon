using Atheon.Services.DiscordHandlers.Autocompleters.DestinyCollectibles;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using Discord;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords;

public class DestinyExcludingRecordDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILogger<DestinyCollectibleDefinitionAutocompleter> _logger;
    private readonly IDestinyDb _destinyDb;

    public DestinyExcludingRecordDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILogger<DestinyCollectibleDefinitionAutocompleter> logger,
        IDestinyDb destinyDb)
    {
        _bungieClientProvider = bungieClientProvider;
        _logger = logger;
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
            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;
            var settings = await _destinyDb.GetGuildSettingsAsync(context.Guild.Id);

            if (settings is null)
                return AutocompletionResult.FromSuccess();

            var searchResults = client
                .Repository
                .GetAll<DestinyRecordDefinition>()
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
