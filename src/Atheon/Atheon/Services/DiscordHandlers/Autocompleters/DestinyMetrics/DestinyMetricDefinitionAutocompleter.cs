using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Extensions;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyMetrics
{
    public class DestinyMetricDefinitionAutocompleter : AutocompleteHandler
    {
        private readonly IBungieClientProvider _bungieClientProvider;
        private readonly ILogger<DestinyMetricDefinitionAutocompleter> _logger;
        private readonly ILocalizationService _localizationService;

        public DestinyMetricDefinitionAutocompleter(
            IBungieClientProvider bungieClientProvider,
            ILogger<DestinyMetricDefinitionAutocompleter> logger,
            ILocalizationService localizationService)
        {
            _bungieClientProvider = bungieClientProvider;
            _logger = logger;
            _localizationService = localizationService;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter, IServiceProvider services)
        {
            try
            {
                var lang = await _localizationService.GetGuildLocale(context.Guild.Id);

                var client = await _bungieClientProvider.GetClientAsync();
                var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

                var searchResults = client
                    .Repository
                    .Search<DestinyMetricDefinition>(x =>
                    {
                        return x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase);
                    }, lang)
                    .Take(20);

                var results = searchResults
                    .Where(x => x.DisplayProperties.Name.Length > 0)
                    .Select(x => new AutocompleteResult(CreateDisplayNameForMetric(x, lang), x.Hash.ToString()));

                return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to form collectibles for query");
                return AutocompletionResult.FromSuccess();
            }
        }

        private static string CreateDisplayNameForMetric(DestinyMetricDefinition metricDefinition, BungieLocales lang)
        {
            if (metricDefinition.Traits.Count != 2)
                return metricDefinition.DisplayProperties.Name;

            var metricTrait = metricDefinition.Traits.Last();

            if (metricTrait.TryGetDefinition(out var traitDefinition, lang))
            {
                return $"{metricDefinition.DisplayProperties.Name} ({traitDefinition.DisplayProperties.Name})";
            }

            return metricDefinition.DisplayProperties.Name;
        }
    }
}
