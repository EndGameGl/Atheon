using Discord;

namespace Atheon.Services.DiscordHandlers.InteractionFlow;

public class DiscordCommandEmbedResult : IDiscordCommandResult
{
    private readonly Embed? _embed;
    private readonly bool _hide;
    private readonly MessageComponent? _components;

    public DiscordCommandEmbedResult(Embed? embed = null, MessageComponent? messageComponent = null, bool hide = false)
    {
        _embed = embed;
        _hide = hide;
        _components = messageComponent;
    }

    public async Task Execute(IInteractionContext interactionContext)
    {
        if (interactionContext.Interaction.HasResponded)
        {
            await interactionContext.Interaction.FollowupAsync(embed: _embed, components: _components, ephemeral: _hide);
        }
        else
        {
            await interactionContext.Interaction.RespondAsync(embed: _embed, components: _components, ephemeral: _hide);
        }
    }
}
