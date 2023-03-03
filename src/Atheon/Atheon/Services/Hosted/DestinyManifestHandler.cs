using Atheon.Services.BungieApi;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;

namespace Atheon.Services.Hosted;

public class DestinyManifestHandler : PeriodicBackgroundService, IDestinyManifestHandler
{
    private readonly ILogger<DestinyManifestHandler> _logger;
    private readonly IBungieClientProvider _bungieClientProvider;

    public bool IsUpdating { get; private set; }
    public event Func<Task> UpdateStarted;

    public DestinyManifestHandler(
        ILogger<DestinyManifestHandler> logger,
        IBungieClientProvider bungieClientProvider) : base(logger)
    {
        _logger = logger;
        _bungieClientProvider = bungieClientProvider;
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

            var hasUpdates = await client.DefinitionProvider.CheckForUpdates();

            if (hasUpdates)
            {
                IsUpdating = true;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                if (UpdateStarted != null)
                {
                    await UpdateStarted();
                }

                _logger.LogInformation("Manifest update started!");

                await client.DefinitionProvider.Update();

                sw.Stop();

                _logger.LogInformation("Manifest update finished in {Time} ms", sw.ElapsedMilliseconds);
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
