using System.Diagnostics;

namespace Atheon.Services.Hosted.Utilities;

/// <summary>
///     Default class to inherit from when the task is supposed to be run on intervals
/// </summary>
public abstract class PeriodicBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly object _timerMutex;
    private long _beforeLastTickExecutedTimestamp;

    private long _iterationsTotal;
    private long _lastTickExecutedTimestamp;
    private PeriodicTimer? _periodicTimer;

    protected PeriodicBackgroundService(
        ILogger logger)
    {
        Stopwatch = new Stopwatch();
        _logger = logger;
        _timerMutex = new object();
    }

    /// <summary>
    ///     Stopwatch used to track all timings
    /// </summary>
    public Stopwatch Stopwatch { get; }

    /// <summary>
    ///     Time taken to execute last tick
    /// </summary>
    public long LastTickExecutedIn { get; private set; }

    public double ApproximateTimeToRunTick { get; private set; }

    /// <summary>
    ///     Run before actual loop starts, use to set up variables
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected abstract Task BeforeExecutionAsync(CancellationToken stoppingToken);

    /// <summary>
    ///     Runs every tick while service is active
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task OnTimerExecuted(CancellationToken cancellationToken);

    protected virtual Task OnExit(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Method responsible for handling task running
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BeforeExecutionAsync(stoppingToken);

        Stopwatch.Start();

        _beforeLastTickExecutedTimestamp = Stopwatch.ElapsedMilliseconds;
        _lastTickExecutedTimestamp = Stopwatch.ElapsedMilliseconds;

        await OnTimerExecuted(stoppingToken);

        while (await GetTimerSafe().WaitForNextTickAsync(stoppingToken))
            try
            {
                _iterationsTotal++;
                _beforeLastTickExecutedTimestamp = Stopwatch.ElapsedMilliseconds;
                await OnTimerExecuted(stoppingToken);
                _lastTickExecutedTimestamp = Stopwatch.ElapsedMilliseconds;
                LastTickExecutedIn = _lastTickExecutedTimestamp - _beforeLastTickExecutedTimestamp;

                ApproximateTimeToRunTick = (ApproximateTimeToRunTick * (_iterationsTotal - 1) + LastTickExecutedIn) /
                                           _iterationsTotal;

                _logger.LogDebug(
                    "Service executed tick in: {LastTickExecutedIn} ms | Median value: {ApproximateTimeToRunTick} ms",
                    LastTickExecutedIn,
                    ApproximateTimeToRunTick.ToString("##.000"));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Encountered exception while executing background service tick");
            }

        _logger.LogInformation("Exited background service loop");

        await OnExit(stoppingToken);
    }

    /// <summary>
    ///     Fetches the timer safely, ensures that the timer exists
    /// </summary>
    /// <returns></returns>
    private PeriodicTimer GetTimerSafe()
    {
        lock (_timerMutex)
        {
            if (_periodicTimer is null)
                throw new Exception("Timer is not supposed to be null");
            return _periodicTimer;
        }
    }

    /// <summary>
    ///     Safely changes timer interval to specified one
    /// </summary>
    /// <param name="newInterval"></param>
    protected void ChangeTimerSafe(TimeSpan newInterval)
    {
        lock (_timerMutex)
        {
            _periodicTimer = new PeriodicTimer(newInterval);
        }

        _logger.LogDebug("Changing service timer loop to interval: {NewInterval}", newInterval);
    }

}
