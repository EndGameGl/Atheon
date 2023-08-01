using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Activities;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyActivities;

public class DestinyActivityDefinitionAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<DestinyActivityDefinitionAutocompleter> _logger;

    public DestinyActivityDefinitionAutocompleter(
        IBungieClientProvider bungieClientProvider,
        ILocalizationService localizationService,
        ILogger<DestinyActivityDefinitionAutocompleter> logger
    )
    {
        _bungieClientProvider = bungieClientProvider;
        _localizationService = localizationService;
        _logger = logger;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services
    )
    {
        try
        {
            var lang = await _localizationService.GetGuildLocaleCachedAsync(context.Guild.Id);
            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)
                autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

            var searchResults = client.Repository
                .GetAll<DestinyActivityDefinition>(lang)
                .Where(
                    x =>
                    {
                        if (x.Redacted)
                            return false;

                        if (x.Matchmaking is null)
                            return false;

                        if (x.Matchmaking.MaxParty < 2)
                            return false;

                        return x.DisplayProperties.Name.Contains(
                            searchEntry,
                            StringComparison.InvariantCultureIgnoreCase
                        );
                    }
                 )
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(
                    x =>
                        new AutocompleteResult(
                            x.DisplayProperties.Name,
                            x.Hash.ToString()
                        )
                );

            return !results.Any()
                ? AutocompletionResult.FromSuccess()
                : AutocompletionResult.FromSuccess(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to form collectibles for query");
            return AutocompletionResult.FromSuccess();
        }
    }
}
