using Atheon.Services.BungieApi;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;

namespace Atheon.Services.Hosted;

public class DestinyManifestHandler : PeriodicBackgroundService, IDestinyManifestHandler
{
    private readonly ILogger<DestinyManifestHandler> _logger;
    private readonly IBungieClientProvider _bungieClientProvider;
    private readonly IDiscordEventHandler _discordEventHandler;
    private readonly DestinyDefinitionDataService _destinyDefinitionDataService;
    private readonly IBungieApiStatus _bungieApiStatus;

    public bool IsUpdating { get; private set; }
    public event Func<Task> UpdateStarted;

    public DestinyManifestHandler(
        ILogger<DestinyManifestHandler> logger,
        IBungieClientProvider bungieClientProvider,
        IDiscordEventHandler discordEventHandler,
        DestinyDefinitionDataService destinyDefinitionDataService,
        IBungieApiStatus bungieApiStatus) : base(logger)
    {
        _logger = logger;
        _bungieClientProvider = bungieClientProvider;
        _discordEventHandler = discordEventHandler;
        _destinyDefinitionDataService = destinyDefinitionDataService;
        _bungieApiStatus = bungieApiStatus;
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

            if (!_bungieApiStatus.IsLive)
                return;

            var client = await _bungieClientProvider.GetClientAsync();

            var hasUpdates = await client.DefinitionProvider.CheckForUpdates();

            if (hasUpdates)
            {
                IsUpdating = true;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                if (UpdateStarted != null)
                {
                    await UpdateStarted();
                }
                await _discordEventHandler.ReportToSystemChannelAsync($"Destiny manifest update started...");
                _logger.LogInformation("Manifest update started!");

                await client.DefinitionProvider.Update();
                client.Repository.Clear();

                var manifest = await client.ApiAccess.Destiny2.GetDestinyManifest();
                await client.DefinitionProvider.ChangeManifestVersion(manifest.Response.Version);

                await client.DefinitionProvider.ReadToRepository(client.Repository);
                await _destinyDefinitionDataService.MapLookupTables();

                sw.Stop();

                _logger.LogInformation("Manifest update finished in {Time} ms", sw.ElapsedMilliseconds);
                await _discordEventHandler.ReportToSystemChannelAsync($"Manifest update finished in {sw.ElapsedMilliseconds} ms");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Encountered error while updating destiny manifest");
        }
        finally
        {
            IsUpdating = false;
        }
    }
}
