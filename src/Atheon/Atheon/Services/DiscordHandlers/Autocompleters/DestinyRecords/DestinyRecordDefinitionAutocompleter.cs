using Atheon.DataAccess;
using Atheon.Extensions;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;

namespace Atheon.Services.DiscordHandlers.Autocompleters.DestinyRecords
{
    public class DestinyRecordDefinitionAutocompleter : AutocompleteHandler
    {
        private readonly IBungieClientProvider _bungieClientProvider;
        private readonly IDestinyDb _destinyDb;
        private readonly IMemoryCache _memoryCache;

        public DestinyRecordDefinitionAutocompleter(
            IBungieClientProvider bungieClientProvider,
            IDestinyDb destinyDb,
            IMemoryCache memoryCache)
        {
            _bungieClientProvider = bungieClientProvider;
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

            var client = await _bungieClientProvider.GetClientAsync();
            var searchEntry = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

            var searchResults = client
                .Repository
                .GetAll<DestinyRecordDefinition>(lang)
                .Where(x => x.DisplayProperties.Name.Contains(searchEntry, StringComparison.InvariantCultureIgnoreCase))
                .Take(20);

            var results = searchResults
                .Where(x => x.DisplayProperties.Name.Length > 0)
                .Select(x => new AutocompleteResult(x.DisplayProperties.Name, x.Hash.ToString()));

            return !results.Any() ? AutocompletionResult.FromSuccess() : AutocompletionResult.FromSuccess(results);
        }
    }
}
