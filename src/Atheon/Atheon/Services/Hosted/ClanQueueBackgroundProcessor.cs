using Atheon.Models.Scanning;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.DestinyClanScanner;
using System.Collections.Concurrent;

namespace Atheon.Services.Hosted;

public class ClanQueueBackgroundProcessor : PeriodicBackgroundService, IClanQueue
{
    private const int MaxParallelClanScans = 10;

    private readonly ILogger<ClanQueueBackgroundProcessor> _logger;
    private readonly IClansToScanProvider _clansToScanProvider;
    private readonly DestinyClanScanner _destinyClanScanner;
    private readonly ConcurrentDictionary<long, OngoingScan> _ongoingScans;

    public ClanQueueBackgroundProcessor(
        ILogger<ClanQueueBackgroundProcessor> logger,
        IClansToScanProvider clansToScanProvider,
        DestinyClanScanner destinyClanScanner) : base(logger)
    {
        _logger = logger;
        _clansToScanProvider = clansToScanProvider;
        _destinyClanScanner = destinyClanScanner;
        _ongoingScans = new ConcurrentDictionary<long, OngoingScan>();
    }

    protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
    {
        ChangeTimerSafe(TimeSpan.FromSeconds(0.1));
        return Task.CompletedTask;
    }

    protected override async Task OnTimerExecuted(CancellationToken cancellationToken)
    {
        ClearOutFinishedScans();
        if (!await StartNewScans())
        {
            await Task.Delay(1000);
        }
    }

    private void ClearOutFinishedScans()
    {
        var finishedTasks = _ongoingScans.Where(x => x.Value.ScanTask.IsCompleted).ToList();

        foreach (var finishedTask in finishedTasks)
        {
            _ongoingScans.TryRemove(finishedTask.Key, out _);
        }
    }

    private async ValueTask<bool> StartNewScans()
    {
        var freeSlots = MaxParallelClanScans - _ongoingScans.Count;
        if (freeSlots > 0)
        {
            var newClans = await _clansToScanProvider.GetClansToScanAsync(freeSlots);

            foreach (var clan in newClans)
            {
                var scanTask = StartClanScan(clan);

                if (!_ongoingScans.TryAdd(
                        clan,
                        new OngoingScan(scanTask, default)))
                {
                    _logger.LogWarning("Failed to add clan to scanning: {Id}",
                        clan);
                }
            }
        }
        return false;
    }

    private async Task<long> StartClanScan(long clanId)
    {      
        await _destinyClanScanner.Scan(new DestinyClanScannerInput() { ClanId = clanId }, new DestinyClanScannerContext(), default);
        return clanId;
    }
}
