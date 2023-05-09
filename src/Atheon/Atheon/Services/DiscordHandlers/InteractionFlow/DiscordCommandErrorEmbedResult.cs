using Atheon.Extensions;
using Discord;
using System;
using System.Drawing;

namespace Atheon.Services.DiscordHandlers.InteractionFlow
{
    public class DiscordCommandErrorEmbedResult : IDiscordCommandResult
    {
        private readonly string _errorMessage;

        public DiscordCommandErrorEmbedResult(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public async Task Execute(IInteractionContext interactionContext)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithCurrentTimestamp();
            embedBuilder.WithFooter("Atheon", "https://www.bungie.net/common/destiny2_content/icons/6d091410227eef82138a162df73065b9.png");
            embedBuilder.WithColor(Discord.Color.Red);

            embedBuilder.WithTitle("Failed to execute command");
            embedBuilder.WithDescription($"```{_errorMessage}```");

            var embed = embedBuilder.Build();
            await interactionContext.Interaction.RespondAsync(embed: embed);
        }
    }
}
