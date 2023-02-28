using Atheon.Models.Scanning;
using Atheon.Services.Hosted.Utilities;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.DestinyClanMemberScanner;
using Atheon.Services.Scanners.DestinyClanScanner;
using System.Collections.Concurrent;

namespace Atheon.Services.Hosted;

public class UserQueueBackgroundProcessor : PeriodicBackgroundService, IUserQueue
{
    private readonly ILogger<UserQueueBackgroundProcessor> _logger;
    private readonly DestinyClanMemberBroadcastedScanner _memberScanner;
    private readonly DestinyClanMemberSilentScanner _silentMemberScanner;
    private readonly IBungieApiStatus _bungieApiStatus;
    private readonly ConcurrentDictionary<long, ClanScanProgress> _scannedClans;
    private readonly ConcurrentQueue<ClanMemberScanEntry> _queue;

    private readonly object _queueLock = new();
    private readonly object _scannedClansLock = new();

    private readonly ConcurrentDictionary<long, OngoingScan> _ongoingScans;

    private const int MaxParallelUserScans = 70;

    public UserQueueBackgroundProcessor(
        ILogger<UserQueueBackgroundProcessor> logger,
        DestinyClanMemberBroadcastedScanner memberScanner,
        DestinyClanMemberSilentScanner silentMemberScanner,
        IBungieApiStatus bungieApiStatus) : base(logger)
    {
        _logger = logger;
        _memberScanner = memberScanner;
        _silentMemberScanner = silentMemberScanner;
        _bungieApiStatus = bungieApiStatus;
        _scannedClans = new ConcurrentDictionary<long, ClanScanProgress>();
        _queue = new ConcurrentQueue<ClanMemberScanEntry>();
        _ongoingScans = new ConcurrentDictionary<long, OngoingScan>();
    }

    protected override Task BeforeExecutionAsync(CancellationToken stoppingToken)
    {
        ChangeTimerSafe(TimeSpan.FromSeconds(0.01));
        return Task.CompletedTask;
    }

    protected override async Task OnTimerExecuted(CancellationToken cancellationToken)
    {
        try
        {
            ClearFinishedUserScans();
            ClearOutFinishedClans();
            if (!_bungieApiStatus.IsLive)
                return;
            StartNewScans();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Encountered error in background loop, timing out for 5s to prevent error spam");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private async Task<long> StartScan(ClanMemberScanEntry entry)
    {
        try
        {
            if (entry.Silent)
            {
                await _silentMemberScanner.Scan(
                    input: new DestinyClanMemberScannerInput()
                    {
                        BungieClient = entry.UpdateContext.BungieClient!,
                        ClanScannerContext = entry.UpdateContext,
                        GroupMember = entry.Member
                    },
                    context: new DestinyClanMemberScannerContext()
                    {

                    },
                    entry.CancellationToken);
            }
            else
            {
                await _memberScanner.Scan(
                    input: new DestinyClanMemberScannerInput()
                    {
                        BungieClient = entry.UpdateContext.BungieClient!,
                        ClanScannerContext = entry.UpdateContext,
                        GroupMember = entry.Member
                    },
                    context: new DestinyClanMemberScannerContext()
                    {

                    },
                    entry.CancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to scan user {MemberName}", entry.Member.DestinyUserInfo.DisplayName);
        }
        finally
        {
            if (_scannedClans.TryGetValue(entry.Member.GroupId, out var sp))
            {
                sp.IncrementCompleted();
            }
        }

        return entry.Member.DestinyUserInfo.MembershipId;
    }


    private void ClearOutFinishedClans()
    {
        try
        {
            var snapshot = GetFromScannedClansThreadSafe((scannedClans) => scannedClans.ToList());

            foreach (var (clanId, clanScanProgress) in snapshot)
            {
                if (clanScanProgress.ScansScheduled <= clanScanProgress.ScansCompleted)
                {
                    clanScanProgress.TaskCompletionSource.SetResult(clanScanProgress);
                    _scannedClans.TryRemove(clanId, out _);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to clear out finished scans");
        }
    }

    private void StartNewScans()
    {
        var currentTime = DateTime.UtcNow;

        if (_ongoingScans.Any(x => (currentTime - x.Value.TimeStarted).TotalSeconds > 10))
            return;

        var freeSlots = MaxParallelUserScans - _ongoingScans.Count;

        if (freeSlots > 0 && TryDequeueMembersForScan(freeSlots, out var newScanEntries))
        {
            Parallel.ForEach(newScanEntries, (scanEntry) =>
            {
                if (scanEntry.CancellationToken.IsCancellationRequested)
                {
                    if (_scannedClans.TryGetValue(scanEntry.Member.GroupId, out var sp))
                    {
                        sp.IncrementCompleted();
                    }

                    return;
                }

                var scan = StartScan(scanEntry);

                if (!_ongoingScans.TryAdd(
                        scanEntry.Member.DestinyUserInfo.MembershipId,
                        new OngoingScan(scan, default)))
                {
                    _logger.LogWarning("Failed to add user to scanning: {Id}",
                        scanEntry.Member.DestinyUserInfo.MembershipId);
                }
            });
        }
    }

    private void ClearFinishedUserScans()
    {
        var finishedTasks = _ongoingScans.Where(x => x.Value.ScanTask.IsCompleted).ToList();

        foreach (var finishedTask in finishedTasks)
        {
            _ongoingScans.TryRemove(finishedTask.Key, out _);
        }
    }

    private bool TryDequeueMembersForScan(int amount, out List<ClanMemberScanEntry>? members)
    {
        members = null;

        if (_queue.Count == 0)
            return false;

        members = GetFromQueueThreadSafe((queue) =>
        {
            var tempCol = new List<ClanMemberScanEntry>();
            while (tempCol.Count != amount)
            {
                if (queue.TryDequeue(out var entry))
                {
                    tempCol.Add(entry);
                }
                else
                {
                    break;
                }
            }

            return tempCol;
        });


        return true;
    }

    public Task<ClanScanProgress> EnqueueAndWaitForBroadcastedUserScans(
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<ClanScanProgress>();

        PerformOnScannedClansThreadSafe((scannedClans) =>
        {
            scannedClans.TryAdd(context.ClanId, new ClanScanProgress()
            {
                ScansScheduled = context.MembersToScan.Count,
                ScansCompleted = 0,
                TaskCompletionSource = tcs
            });
        });

        PerformOnQueueThreadSafe((queue) =>
        {
            foreach (var member in context.MembersToScan)
            {
                queue.Enqueue(new ClanMemberScanEntry()
                {
                    Member = member,
                    UpdateContext = context,
                    Silent = false,
                    CancellationToken = cancellationToken
                });
            }
        });

        return tcs.Task;

    }

    public Task<ClanScanProgress> EnqueueAndWaitForSilentUserScans(
        DestinyClanScannerContext context,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<ClanScanProgress>();

        PerformOnScannedClansThreadSafe((scannedClans) =>
        {
            scannedClans.TryAdd(context.ClanId, new ClanScanProgress()
            {
                ScansScheduled = context.MembersToScan.Count,
                ScansCompleted = 0,
                TaskCompletionSource = tcs
            });
        });

        PerformOnQueueThreadSafe((queue) =>
        {
            foreach (var member in context.MembersToScan)
            {
                queue.Enqueue(new ClanMemberScanEntry()
                {
                    Member = member,
                    UpdateContext = context,
                    Silent = true,
                    CancellationToken = cancellationToken
                });
            }
        });

        return tcs.Task;

    }

    private T GetFromScannedClansThreadSafe<T>(Func<ConcurrentDictionary<long, ClanScanProgress>, T> action)
    {
        lock (_scannedClansLock)
        {
            return action(_scannedClans);
        }
    }

    private void PerformOnScannedClansThreadSafe(Action<ConcurrentDictionary<long, ClanScanProgress>> action)
    {
        lock (_scannedClansLock)
        {
            action(_scannedClans);
        }
    }

    private void PerformOnQueueThreadSafe(Action<ConcurrentQueue<ClanMemberScanEntry>> action)
    {
        lock (_queueLock)
        {
            action(_queue);
        }
    }

    private T GetFromQueueThreadSafe<T>(Func<ConcurrentQueue<ClanMemberScanEntry>, T> action)
    {
        lock (_queueLock)
        {
            return action(_queue);
        }
    }

}
