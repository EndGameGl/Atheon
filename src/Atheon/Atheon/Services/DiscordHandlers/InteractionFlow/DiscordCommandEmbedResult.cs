using Discord;

namespace Atheon.Services.DiscordHandlers.InteractionFlow;

public class DiscordCommandEmbedResult : IDiscordCommandResult
{
    private readonly Embed _embed;
    private readonly bool _hide;

    public DiscordCommandEmbedResult(Embed embed, bool hide) 
    { 
        _embed = embed;
        _hide = hide;
    }

    public async Task Execute(IInteractionContext interactionContext)
    {
        await interactionContext.Interaction.RespondAsync(embed: _embed, ephemeral: _hide);
    }
}
