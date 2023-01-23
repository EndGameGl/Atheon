namespace Atheon.Models.Scanning;

public class ClanScanProgress
{
    public TaskCompletionSource<ClanScanProgress> TaskCompletionSource { get; init; }
    public int ScansScheduled { get; set; }

    private readonly object _lock = new();

    public int ScansCompleted { get; set; }

    public void IncrementCompleted()
    {
        lock (_lock)
        {
            ScansCompleted++;
        }
    }

}
