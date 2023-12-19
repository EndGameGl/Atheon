using Atheon.DataAccess;
using Atheon.DataAccess.Models.Destiny;
using Atheon.Models.Destiny;
using Atheon.Services.DiscordHandlers.TypeConverters;
using Atheon.Services.Interfaces;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetBungieAPI.Models;
using System.Diagnostics;
using System.Reflection;

namespace Atheon.Services.DiscordHandlers;

public class DiscordEventHandler : IDiscordEventHandler
{
	private readonly IDiscordClientProvider _discordClientProvider;
	private readonly IDestinyDb _destinyDb;
	private readonly IGuildDb _guildDb;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DiscordEventHandler> _logger;
	private readonly EmbedBuilderService _embedBuilderService;

	private InteractionService _interactionService;
	private DiscordShardedClient _discordClient;

	public DiscordEventHandler(
		IDiscordClientProvider discordClientProvider,
		IDestinyDb destinyDb,
		IGuildDb guildDb,
		IServiceProvider serviceProvider,
		ILogger<DiscordEventHandler> logger,
		EmbedBuilderService embedBuilderService
	)
	{
		_discordClientProvider = discordClientProvider;
		_destinyDb = destinyDb;
		_guildDb = guildDb;
		_serviceProvider = serviceProvider;
		_logger = logger;
		_embedBuilderService = embedBuilderService;
	}

	private async Task RegisterInteractions()
	{
		try
		{
			_interactionService.AddTypeConverter<DestinyProfilePointer>(
				new DestinyProfilePointerTypeConverter()
			);
			// This does some magic and finds all references of [SlashCommand("name", "description")] in the project and links them to the interaction service.
			await _interactionService.AddModulesAsync(
				Assembly.GetEntryAssembly(),
				_serviceProvider
			);

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

		_interactionService = new InteractionService(
			_discordClient,
			new InteractionServiceConfig()
			{
				LocalizationManager = new JsonLocalizationManager(
					"./Localization/DiscordCommands/",
					"commands"
				)
			}
		);

		_discordClient.Log += OnDiscordLog;

		_discordClient.JoinedGuild += OnGuildJoin;
		_discordClient.LeftGuild += OnGuildLeft;

		_discordClient.SlashCommandExecuted += HandleCommand;
		_discordClient.ButtonExecuted += HandleCommand;
		_discordClient.SelectMenuExecuted += HandleCommand;
		_discordClient.AutocompleteExecuted += HandleCommand;

		RegisterInteractions().GetAwaiter().GetResult();
	}

	private async Task HandleCommand<TInteraction>(TInteraction interaction)
		where TInteraction : SocketInteraction
	{
		_logger.LogInformation(
			"[Discord] Executing interaction: {InteractionType}",
			interaction.Type
		);
		var context = new ShardedInteractionContext<TInteraction>(_discordClient, interaction);
		var sw = Stopwatch.StartNew();
		var result = await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
		sw.Stop();
		if (!result.IsSuccess)
		{
			_logger.LogError(
				"[Discord] Failed to run command in {Time} ms due to {ErrorType}: {ErrorReason}",
				sw.ElapsedMilliseconds,
				result.Error,
				result.ErrorReason
			);
		}
		else
		{
			_logger.LogInformation(
				"[Discord] Completed command in {Time} ms",
				sw.ElapsedMilliseconds
			);
		}
	}

	private async Task OnGuildJoin(SocketGuild socketGuild)
	{
		var newGuildData = DiscordGuildSettingsDbModel.CreateDefault(
			socketGuild.Id,
			socketGuild.Name
		);

		await _guildDb.UpsertGuildSettingsAsync(newGuildData);
	}

	private async Task OnGuildLeft(SocketGuild socketGuild)
	{
		await _guildDb.DeleteGuildSettingsAsync(socketGuild.Id);
	}

	private async Task OnDiscordLog(LogMessage logMessage)
	{
		switch (logMessage.Severity)
		{
			case LogSeverity.Critical:
				_logger.LogCritical(
					logMessage.Exception,
					"[Discord] {Source}: {Message}",
					logMessage.Source,
					logMessage.Message
				);
				break;
			case LogSeverity.Error:
				_logger.LogError(
					logMessage.Exception,
					"[Discord] {Source}: {Message}",
					logMessage.Source,
					logMessage.Message
				);
				break;
			case LogSeverity.Warning:
				_logger.LogWarning(
					"[Discord] {Source}: {Message}",
					logMessage.Source,
					logMessage.Message
				);
				break;
			case LogSeverity.Info:
				_logger.LogInformation(
					"[Discord] {Source}: {Message}",
					logMessage.Source,
					logMessage.Message
				);
				break;
			case LogSeverity.Verbose:
				_logger.LogDebug(
					"[Discord] {Source}: {Message}",
					logMessage.Source,
					logMessage.Message
				);
				break;
			case LogSeverity.Debug:
				_logger.LogDebug(
					"[Discord] {Source}: {Message}",
					logMessage.Source,
					logMessage.Message
				);
				break;
		}
	}

	public async Task ReportToSystemChannelAsync(string message)
	{
		var guildSettings = await _guildDb.GetAllGuildSettings();

		var embed = _embedBuilderService
			.CreateSimpleResponseEmbed("System alert", message, Color.Orange)
			.Build();
		foreach (var guildSetting in guildSettings)
		{
			if (guildSetting is not null && guildSetting.SystemReportsEnabled)
			{
				var channelToReportTo =
					guildSetting.SystemReportsOverrideChannel ?? guildSetting.DefaultReportChannel;
				if (channelToReportTo is null)
				{
					continue;
				}

				if (!_discordClientProvider.IsReady)
				{
					continue;
				}

				var client = _discordClientProvider.Client!;
				var guild = client.GetGuild(guildSetting.GuildId);
				if (guild is null)
					continue;

				var textChannel = guild.GetTextChannel(channelToReportTo.Value);
				await textChannel.SendMessageAsync(embed: embed);
			}
		}
	}

	public async Task ReportGlobalAlertToSystemChannelAsync(GlobalAlert alert)
	{
		var guildSettings = await _guildDb.GetAllGuildSettings();

		var embedBuilder = _embedBuilderService
			.CreateSimpleResponseEmbed(
				$"Global alert: {alert.AlertKey}",
				alert.AlertHtml,
				GetAlertColor(alert.AlertLevel)
			)
			.WithUrl(alert.AlertLink)
			.WithTimestamp(alert.AlertTimestamp);

		var embed = embedBuilder.Build();

		foreach (var guildSetting in guildSettings)
		{
			if (guildSetting is not null && guildSetting.SystemReportsEnabled)
			{
				var channelToReportTo =
					guildSetting.SystemReportsOverrideChannel ?? guildSetting.DefaultReportChannel;
				if (channelToReportTo is null)
				{
					continue;
				}

				if (!_discordClientProvider.IsReady)
				{
					continue;
				}

				var client = _discordClientProvider.Client!;
				var guild = client.GetGuild(guildSetting.GuildId);
				if (guild is null)
					continue;

				var textChannel = guild.GetTextChannel(channelToReportTo.Value);
				await textChannel.SendMessageAsync(embed: embed);
			}
		}
	}

	private Discord.Color GetAlertColor(GlobalAlertLevel level)
	{
		return (level) switch
		{
			GlobalAlertLevel.Red => Color.Red,
			GlobalAlertLevel.Yellow => Color.Gold,
			GlobalAlertLevel.Blue => Color.Blue,
			_ => Color.Orange
		};
	}
}
