using Atheon.Models.Database.Destiny;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace Atheon.Services.DiscordHandlers;

public class DiscordEventHandler : IDiscordEventHandler
{
    private readonly IDiscordClientProvider _discordClientProvider;
    private readonly IDestinyDb _destinyDb;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordEventHandler> _logger;

    private InteractionService _interactionService;
    private DiscordShardedClient _discordClient;

    public DiscordEventHandler(
        IDiscordClientProvider discordClientProvider,
        IDestinyDb destinyDb,
        IServiceProvider serviceProvider,
        ILogger<DiscordEventHandler> logger)
    {
        _discordClientProvider = discordClientProvider;
        _destinyDb = destinyDb;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private async Task RegisterInteractions()
    {
        try
        {
            // This does some magic and finds all references of [SlashCommand("name", "description")] in the project and links them to the interaction service.
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            // This registers all the above found SlashCommands to this specific guild, for testing.
            await _interactionService.RegisterCommandsToGuildAsync(1077105046044545024, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register discord interactions");
        }
    }

    public void SubscribeToEvents()
    {
        _logger.LogInformation("Setting up discord event handlers...");
        if (!_discordClientProvider.IsReady)
        {
            return;
        }

        _discordClient = _discordClientProvider.Client!;

        _interactionService = new InteractionService(_discordClient, new InteractionServiceConfig()
        {

        });

        _discordClient.JoinedGuild += OnGuildJoin;
        _discordClient.LeftGuild += OnGuildLeft;

        _discordClient.SlashCommandExecuted += HandleCommand;
        _discordClient.ButtonExecuted += HandleCommand;
        _discordClient.SelectMenuExecuted += HandleCommand;
        _discordClient.AutocompleteExecuted += HandleCommand;

        RegisterInteractions().GetAwaiter().GetResult();
    }

    private async Task HandleCommand<TInteraction>(TInteraction interaction) where TInteraction : SocketInteraction
    {
        _logger.LogInformation("Executing discord interaction: {InteractionType}", interaction.Type);
        var context = new ShardedInteractionContext<TInteraction>(_discordClient, interaction);
        await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
    }

    private async Task OnGuildJoin(SocketGuild socketGuild)
    {
        var newGuildData = DiscordGuildSettingsDbModel.CreateDefault(socketGuild.Id, socketGuild.Name);

        await _destinyDb.UpsertGuildSettingsAsync(newGuildData);
    }

    private async Task OnGuildLeft(SocketGuild socketGuild)
    {
        await _destinyDb.DeleteGuildSettingsAsync(socketGuild.Id);
    }
}
