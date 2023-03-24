using Atheon.Services.BungieApi;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using DotNetBungieAPI.Models.Common;

namespace Atheon.Services.Hosted;

public class BungieLifecheckService : PeriodicBackgroundService, IBungieApiStatus
{
    private readonly ILogger<BungieLifecheckService> _logger;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly BungieNetApiCallHandler _bungieNetApiCallHandler;
    private readonly IDiscordEventHandler _discordEventHandler;

    public bool IsLive { get; private set; }
    public event Func<bool, Task>? StatusChanged;
    public CoreSettingsConfiguration? LatestSettingsResponse { get; private set; }
    public DateTime? LatestSettingsResponseDate { get; private set; }


    public BungieLifecheckService(
        ILogger<BungieLifecheckService> logger,
        IBungieClientProvider bungieClientProvider,
        BungieNetApiCallHandler bungieNetApiCallHandler,
        IDiscordEventHandler discordEventHandler) : base(logger)
    {
        _logger = logger;
        _bungieClientProvider = bungieClientProvider;
        _bungieNetApiCallHandler = bungieNetApiCallHandler;
        _discordEventHandler = discordEventHandler;
    }

    protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
    {
        ChangeTimerSafe(TimeSpan.FromSeconds(15));
        return Task.CompletedTask;
    }

    protected override async Task OnTimerExecuted(CancellationToken cancellationToken)
    {
        try
        {
            if (!_bungieClientProvider.IsReady)
                return;

            var client = await _bungieClientProvider.GetClientAsync();

            var settings = await _bungieNetApiCallHandler.PerformRequestAndLog(async (handler) => await client.ApiAccess.Misc.GetCommonSettings());

            if (!settings.IsSuccessfulResponseCode)
            {
                await SetStatusAndReport(status: false);
                return;
            }

            LatestSettingsResponse = settings.Response;
            LatestSettingsResponseDate = DateTime.UtcNow;

            var settingsConfiguration = settings.Response;

            if (!settingsConfiguration.Systems.TryGetValue("D2Profiles", out var profilesSystem))
            {
                await SetStatusAndReport(status: false);
                return;
            }

            await SetStatusAndReport(status: profilesSystem.IsEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking is bungie api status");
        }
    }

    private async Task SetStatusAndReport(bool status)
    {
        if (IsLive != status)
        {
            IsLive = status;
            if (StatusChanged != null)
            {
                await StatusChanged(IsLive);
            }
            await _discordEventHandler.ReportToSystemChannelAsync($"Bungie api status change: {(IsLive ? "Live" : "Down")}");
        }
    }

}
