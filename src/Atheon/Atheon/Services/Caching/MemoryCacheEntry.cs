namespace Atheon.Services.Caching;

public class MemoryCacheEntry
{
    public object? Value { get; set; }
    public DateTime DateStored { get; set; }
    public TimeSpan TimeToStore { get; set; }

    public bool DidExpire(ref DateTime currentTime)
    {
        var dueTime = DateStored + TimeToStore;
        return currentTime >= dueTime;
    }

    public void UpdateValue(object value)
    {
        Value = value;
        DateStored = DateTime.UtcNow;
    }
}
