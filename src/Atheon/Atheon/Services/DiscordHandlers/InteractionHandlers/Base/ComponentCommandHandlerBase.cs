using Atheon.Services.DiscordHandlers.InteractionFlow;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetBungieAPI.Models.Destiny;

namespace Atheon.Services.DiscordHandlers.InteractionHandlers.Base;

public abstract class ComponentCommandHandlerBase : InteractionModuleBase<ShardedInteractionContext<SocketMessageComponent>>
{
    protected ulong GuildId => Context.Guild.Id;

    protected EmbedBuilderService EmbedBuilderService { get; }

    private readonly ILogger _logger;

    public ComponentCommandHandlerBase(
        ILogger logger,
        EmbedBuilderService embedBuilderService)
    {
        _logger = logger;
        EmbedBuilderService = embedBuilderService;
    }

    public override Task BeforeExecuteAsync(ICommandInfo command)
    {
        _logger.LogInformation("Started executing slash command: {CommandName}", command.Name);
        return Task.CompletedTask;
    }

    public override Task AfterExecuteAsync(ICommandInfo command)
    {
        _logger.LogInformation("Finished executing slash command: {CommandName}", command.Name);
        return Task.CompletedTask;
    }

    private async Task DeferIfTimedOut(TimeSpan timeLeft, CancellationToken cancellationToken)
    {
        await Task.Delay(timeLeft, cancellationToken);
        if (!Context.Interaction.HasResponded)
        {
            _logger.LogInformation("Command taking too long, deferring");
            await Context.Interaction.DeferAsync();
        }
    }

    protected TimeSpan GetTimeLeft()
    {
        var deadline = Context.Interaction.CreatedAt.UtcDateTime.AddSeconds(3);
        var currentTime = DateTime.UtcNow;
        return deadline - currentTime;
    }

    protected async Task ExecuteAndHandleErrors(Func<Task<IDiscordCommandResult>> commandResult)
    {
        try
        {
            var timeLeft = GetTimeLeft();
            var cts = new CancellationTokenSource(timeLeft);

            _ = DeferIfTimedOut(timeLeft.Subtract(TimeSpan.FromSeconds(0.5)), cts.Token);
            var result = await commandResult();
            cts.Cancel();
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

    protected IDiscordCommandResult DestinyDefinitionNotFound<TDefinition>(uint hash) where TDefinition : IDestinyDefinition
    {
        var type = typeof(TDefinition);
        if (this is LocalizedComponentCommandHandlerBase localizedSlashCommandHandler)
        {
            return new DiscordCommandErrorEmbedResult(localizedSlashCommandHandler.FormatText(
                "DefinitionNotFoundError",
                () => "Definition {0} {1} not found",
                type.Name,
                hash));
        }
        return new DiscordCommandErrorEmbedResult($"Definition {type.Name} {hash} not found");
    }

    protected IDiscordCommandResult GuildSettingsNotFound()
    {
        if (this is LocalizedComponentCommandHandlerBase localizedSlashCommandHandler)
        {
            return new DiscordCommandErrorEmbedResult(localizedSlashCommandHandler.Text("GuildSettingsNotFound", () => "Failed to get load guild settings"));
        }
        return new DiscordCommandErrorEmbedResult("Failed to get load guild settings");
    }

    protected IDiscordCommandResult Success(EmbedBuilder? embedBuilder = null, ComponentBuilder? componentBuilder = null, bool hide = false)
    {
        return new DiscordCommandEmbedResult(embedBuilder?.Build(), componentBuilder?.Build(), hide);
    }

    protected IDiscordCommandResult Edit(EmbedBuilder? embedBuilder = null, ComponentBuilder? componentBuilder = null)
    {
        return new DiscordCommandEditResult(embedBuilder?.Build(), componentBuilder?.Build());
    }
}
