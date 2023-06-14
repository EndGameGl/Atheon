using Atheon.DataAccess;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters
{
    public class DestinyClanFromGuildAutocompleter : AutocompleteHandler
    {
        private readonly IDestinyDb _destinyDb;

        public DestinyClanFromGuildAutocompleter(IDestinyDb destinyDb)
        {
            _destinyDb = destinyDb;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var guildId = context.Guild.Id;
            var guildClans = await _destinyDb.GetClanReferencesFromGuildAsync(guildId);
            if (guildClans is null or { Count: 0 })
                return AutocompletionResult.FromSuccess();
            return AutocompletionResult.FromSuccess(guildClans.Select(x => new AutocompleteResult(x.Name, x.Id.ToString())));
        }
    }
}
