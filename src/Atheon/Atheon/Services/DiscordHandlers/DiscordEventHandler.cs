﻿using Atheon.Models.Database.Destiny;
using Atheon.Models.Destiny;
using Atheon.Services.DiscordHandlers.TypeConverters;
using Atheon.Services.Interfaces;
using Discord.Interactions;
using Discord.WebSocket;
using System.Diagnostics;
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
            _interactionService.AddTypeConverter<DestinyProfilePointer>(new DestinyProfilePointerTypeConverter());
            // This does some magic and finds all references of [SlashCommand("name", "description")] in the project and links them to the interaction service.
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

            await _interactionService.RegisterCommandsGloballyAsync(deleteMissing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Discord] Failed to register interactions");
        }
    }

    public void SubscribeToEvents()
    {
        _logger.LogInformation("[Discord] Setting up event handlers...");
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
        _logger.LogInformation("[Discord] Executing interaction: {InteractionType}", interaction.Type);
        var context = new ShardedInteractionContext<TInteraction>(_discordClient, interaction);
        var sw = Stopwatch.StartNew();
        var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
        sw.Stop();
        if (!result.IsSuccess)
        {
            _logger.LogError("[Discord] Failed to run command in {Time}ms due to {ErrorType}: {ErrorReason}", sw.ElapsedMilliseconds, result.Error, result.ErrorReason);
        }
        else
        {
            _logger.LogInformation("[Discord] Completed command in {Time}ms", sw.ElapsedMilliseconds);
        }
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
