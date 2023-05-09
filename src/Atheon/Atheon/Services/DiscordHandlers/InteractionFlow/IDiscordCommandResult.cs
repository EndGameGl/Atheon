using Discord;

namespace Atheon.Services.DiscordHandlers.InteractionFlow;

public interface IDiscordCommandResult
{
    Task Execute(IInteractionContext interactionContext);
}
