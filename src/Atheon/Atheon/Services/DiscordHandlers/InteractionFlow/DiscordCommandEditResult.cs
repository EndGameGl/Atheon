using Discord;

namespace Atheon.Services.DiscordHandlers.InteractionFlow;

public class DiscordCommandEditResult : IDiscordCommandResult
{
    private readonly Embed? _embed;
    private readonly MessageComponent? _components;

    public DiscordCommandEditResult(Embed? embed, MessageComponent? components)
    {
        _embed = embed;
        _components = components;
    }

    public async Task Execute(IInteractionContext interactionContext)
    {
        await interactionContext.Interaction.DeferAsync();

        await interactionContext.Interaction.ModifyOriginalResponseAsync(x =>
        {
            if (_embed is not null)
            {
                x.Embed = _embed;
            }

            if (_components is not null)
            {
                x.Components = _components;
            }
        });
    }
}
