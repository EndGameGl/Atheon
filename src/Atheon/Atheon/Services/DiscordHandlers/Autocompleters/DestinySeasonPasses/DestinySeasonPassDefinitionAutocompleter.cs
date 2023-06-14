using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.SeasonPasses;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinySeasonPasses
{
    public class DestinySeasonPassDefinitionAutocompleter : AutocompleteHandler
    {
        private readonly IBungieClientProvider _bungieClientProvider;
        private readonly ILocalizationService _localizationService;

        public DestinySeasonPassDefinitionAutocompleter(
            IBungieClientProvider bungieClientProvider,
            ILocalizationService localizationService)
        {
            _bungieClientProvider = bungieClientProvider;
            _localizationService = localizationService;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var lang = await _localizationService.GetGuildLocaleCachedAsync(context.Guild.Id);

            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

            var searchResults = client
                .Repository
                .GetAll<DestinySeasonPassDefinition>(lang)
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(x.DisplayProperties.Name, x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
    }
}
