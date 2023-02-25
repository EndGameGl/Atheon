using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;

namespace Atheon.Services.DiscordHandlers.Autocompleters
{
    public class DestinyRecordDefinitionAutocompleter : AutocompleteHandler
    {
        private readonly IBungieClientProvider _bungieClientProvider;

        public DestinyRecordDefinitionAutocompleter(
            IBungieClientProvider bungieClientProvider)
        {
            _bungieClientProvider = bungieClientProvider;
        }
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

            var searchResults = client
                .Repository
                .GetAll<DestinyRecordDefinition>()
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(x.DisplayProperties.Name, x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
    }
}
