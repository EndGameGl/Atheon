using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;

namespace Atheon.Services.DiscordHandlers.Autocompleters;

public class DestinyClanByIdAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;

    public DestinyClanByIdAutocompleter(IBungieClientProvider bungieClientProvider)
    {
        _bungieClientProvider = bungieClientProvider;
    }

    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var apiClient = await _bungieClientProvider.GetClientAsync();

        var input = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

        var clanId = long.Parse(input);

        var groupResponse = await apiClient.ApiAccess.GroupV2.GetGroup(clanId);

        var resultList = new List<AutocompleteResult>
        {
            new AutocompleteResult(groupResponse.Response.Detail.Name, input)
        };

        return !groupResponse.IsSuccessfulResponseCode ?
            AutocompletionResult.FromSuccess() :
            AutocompletionResult.FromSuccess(resultList);
    }
}
