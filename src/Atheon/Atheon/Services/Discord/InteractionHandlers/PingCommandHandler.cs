using Discord.Interactions;
using Discord.WebSocket;

namespace Atheon.Services.Discord.InteractionHandlers;

public class PingCommandHandler : InteractionModuleBase<ShardedInteractionContext<SocketSlashCommand>>
{
    public PingCommandHandler()
    {

    }

    [SlashCommand("ping", "pongs back")]
    public async Task PingPongAsync()
    {
        await Context.Interaction.RespondAsync("pong");
    }
}
