using Atheon.Models.Metrics;
using DotNetBungieAPI.Models;
using System.Collections.Concurrent;

namespace Atheon.Services.BungieApi;

public class BungieNetApiCallLogger
{
    public ulong TotalRequestsMade { get; private set; }
    public ulong TotalErrors { get; private set; }
    public ulong TimeoutsAmount { get; private set; }
    public ConcurrentDictionary<PlatformErrorCodes, ulong> PerErrorRequests { get; private set; }
    public ulong SuccessfulRequests { get; private set; }
    public DateTime MeasureStartTime { get; private set; }

    public BungieNetApiCallLogger()
    {
        PerErrorRequests = new ConcurrentDictionary<PlatformErrorCodes, ulong>();
        MeasureStartTime = DateTime.UtcNow;
    }

    public void LogTimeout()
    {
        TimeoutsAmount++;
        TotalRequestsMade++;
        TotalErrors++;
    }

    public void LogFailure(PlatformErrorCodes errorCode)
    {
        TotalRequestsMade++;
        PerErrorRequests.AddOrUpdate(
            errorCode,
            (_) => 1,
            (_, oldValue) => oldValue + 1);

        TotalErrors++;
    }

    public void LogRequest<T>(BungieResponse<T> response)
    {
        TotalRequestsMade++;
        if (response.IsSuccessfulResponseCode)
        {
            SuccessfulRequests++;
            return;
        }

        PerErrorRequests.AddOrUpdate(
            response.ErrorCode,
            (_) => 1,
            (_, oldValue) => oldValue + 1);

        TotalErrors++;
    }

    public BungieNetApiMetrics TakeSnapshotAndClear()
    {
        var timeSpent = DateTime.UtcNow - MeasureStartTime;
        var reqSpeed = TotalRequestsMade / timeSpent.TotalSeconds;

        var metrics = new BungieNetApiMetrics()
        {
            SuccessfulRequests = SuccessfulRequests,
            TotalErrors = TotalErrors,
            TimeoutsAmount = TimeoutsAmount,
            PerErrorRequests = new Dictionary<PlatformErrorCodes, ulong>(PerErrorRequests),
            TotalRequestsMade = TotalRequestsMade,
            RequestSpeed = reqSpeed,
            ErrorPercentage = TotalErrors == 0 ? 0 : (double)TotalErrors / TotalRequestsMade,
            Uptime = timeSpent
        };

        ResetStats();

        return metrics;
    }

    private void ResetStats()
    {
        SuccessfulRequests = 0;
        TimeoutsAmount = 0;
        TotalErrors = 0;
        PerErrorRequests.Clear();
        TotalRequestsMade = 0;
        MeasureStartTime = DateTime.UtcNow;
    }

}
