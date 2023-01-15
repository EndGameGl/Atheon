using Atheon.Services.Discord.InteractionHandlers.Base;
using Discord.Interactions;
using Discord.WebSocket;

namespace Atheon.Services.Discord.InteractionHandlers;

public class PingCommandHandler : SlashCommandHandlerBase
{
    public PingCommandHandler(ILogger<PingCommandHandler> logger) : base(logger)
    {

    }

    [SlashCommand("ping", "pongs back")]
    public async Task PingPongAsync()
    {
        await Context.Interaction.RespondAsync("pong");
    }
}
