using Atheon.Services.BungieApi;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models;
using System.Collections.ObjectModel;

namespace Atheon.Services.Hosted;

public class BungieAlertService : PeriodicBackgroundService, IBungieAlertService
{
    private readonly ILogger<BungieAlertService> _logger;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly BungieNetApiCallHandler _bungieNetApiCallHandler;
    private readonly IDiscordEventHandler _discordEventHandler;

    public IReadOnlyCollection<GlobalAlert> CurrentAlerts { get; private set; }

    public BungieAlertService(
        ILogger<BungieAlertService> logger,
        IBungieClientProvider bungieClientProvider,
        BungieNetApiCallHandler bungieNetApiCallHandler,
        IDiscordEventHandler discordEventHandler
    )
        : base(logger)
    {
        _logger = logger;
        _bungieClientProvider = bungieClientProvider;
        _bungieNetApiCallHandler = bungieNetApiCallHandler;
        _discordEventHandler = discordEventHandler;
        CurrentAlerts = [];
    }

    protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
    {      
        ChangeTimerSafe(TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    protected override async Task OnTimerExecuted(CancellationToken cancellationToken)
    {
        try
        {
            if (!_bungieClientProvider.IsReady)
                return;

            var client = await _bungieClientProvider.GetClientAsync();

            var globalAlertsResponse = await _bungieNetApiCallHandler.PerformRequestAndLog(async (handler) => await client.ApiAccess.Misc.GetGlobalAlerts());

            if (!globalAlertsResponse.IsSuccessfulResponseCode)
            {
                return;
            }

            var globalAlerts = globalAlertsResponse.Response;

            if (globalAlerts.Length is 0)
            {
                CurrentAlerts = [];
                return;
            }

            if (CurrentAlerts.Count is 0)
            {
                foreach (var globalAlert in globalAlerts)
                {
                    await _discordEventHandler.ReportGlobalAlertToSystemChannelAsync(globalAlert);
                }
                CurrentAlerts = new ReadOnlyCollection<GlobalAlert>(globalAlerts);
            }
            else
            {
                foreach (var globalAlert in globalAlerts)
                {
                    var existingAlert = CurrentAlerts.FirstOrDefault(x => x.AlertKey == globalAlert.AlertKey);
                    if (existingAlert is null)
                        await _discordEventHandler.ReportGlobalAlertToSystemChannelAsync(globalAlert);
                }
                CurrentAlerts = new ReadOnlyCollection<GlobalAlert>(globalAlerts);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking is bungie alerts status");
        }
    }
}
