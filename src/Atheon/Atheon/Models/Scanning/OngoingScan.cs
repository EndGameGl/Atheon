namespace Atheon.Models.Scanning;

public readonly struct OngoingScan
{
    public Task<long> ScanTask { get; }
    public CancellationTokenSource CancellationTokenSource { get; }
    public DateTime TimeStarted { get; } = DateTime.UtcNow;

    public OngoingScan(Task<long> scanTask, CancellationTokenSource cancellationTokenSource)
    {
        ScanTask = scanTask;
        CancellationTokenSource = cancellationTokenSource;
    }

}
