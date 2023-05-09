using Atheon.Extensions;
using Atheon.Services.DiscordHandlers.InteractionFlow;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Polly;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers.Base
{
    public abstract class SlashCommandHandlerBase : InteractionModuleBase<ShardedInteractionContext<SocketSlashCommand>>
    {
        protected ulong GuildId => Context.Guild.Id;

        protected EmbedBuilderService EmbedBuilderService { get; }

        private readonly ILogger _logger;

        public SlashCommandHandlerBase(
            ILogger logger,
            EmbedBuilderService embedBuilderService)
        {
            _logger = logger;
            EmbedBuilderService = embedBuilderService;
        }

        public override void BeforeExecute(ICommandInfo command)
        {
            _logger.LogInformation("Started executing command: {CommandName}", command.Name);
        }

        public override void AfterExecute(ICommandInfo command)
        {
            _logger.LogInformation("Finished executing command: {CommandName}", command.Name);
        }

        protected async Task ExecuteAndHanldeErrors(Func<Task> actualCommand)
        {
            try
            {             
                await actualCommand();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing command");
                var embed = EmbedBuilderService.CreateErrorEmbed(ex);

                if (Context.Interaction.HasResponded)
                {
                    await Context.Interaction.FollowupAsync(embed: embed);
                }
                else
                {
                    await Context.Interaction.RespondAsync(embed: embed);
                }
            }
        }

        protected async Task ExecuteAndHandleErrors(Func<Task<IDiscordCommandResult>> commandResult)
        {
            try
            {
                var result = await commandResult();
                await result.Execute(Context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing command");
                var embed = EmbedBuilderService.CreateErrorEmbed(ex);

                if (Context.Interaction.HasResponded)
                {
                    await Context.Interaction.FollowupAsync(embed: embed);
                }
                else
                {
                    await Context.Interaction.RespondAsync(embed: embed);
                }
            }
        }

        protected IDiscordCommandResult Error(string message)
        {
            return new DiscordCommandErrorEmbedResult(message);
        }

        protected IDiscordCommandResult Success(Embed embed, bool hide)
        {
            return new DiscordCommandEmbedResult(embed, hide);
        }
    }
}
