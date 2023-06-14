using Atheon.Destiny2.Metadata;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using DotNetBungieAPI.Models.Requests;

namespace Atheon.Services.DiscordHandlers.Autocompleters;

public class SearchDestinyUserByNameAutocompleter : AutocompleteHandler
{
    private readonly IBungieClientProvider _bungieClientProvider;

    public SearchDestinyUserByNameAutocompleter(IBungieClientProvider bungieClientProvider)
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

        var input = (string)autocompleteInteraction.Data.Options.First(x => x.Focused).Value;

        var response = await client.ApiAccess.User.SearchByGlobalNamePost(new UserSearchPrefixRequest(input));

        if (!response.IsSuccessfulResponseCode)
        {
            return AutocompletionResult.FromSuccess();
        }

        var data = response.Response.SearchResults.SelectMany(
            x => x.DestinyMemberships.Select(m => new AutocompleteResult(
                $"{m.BungieGlobalDisplayName}#{m.BungieGlobalDisplayNameCode:D4} ({m.MembershipType.ToFriendlyType()})",
                $"{m.MembershipId}-{(int)m.MembershipType}")))
            .Take(25);

        return AutocompletionResult.FromSuccess(data);
    }
}
