using System.Collections.Concurrent;

namespace Atheon.Services.Scanners.Entities;

public static class ScanEventHandler<T> where T : ScanEventArgs
{
    private static ConcurrentDictionary<Func<T, ValueTask>, object?> _handlers;

    static ScanEventHandler()
    {
        _handlers = new ConcurrentDictionary<Func<T, ValueTask>, object?>();
    }

    public static void Subscribe(Func<T, ValueTask> handler)
    {
        _handlers.TryAdd(handler, null);
    }

    private static void Unsubscribe(Func<T, ValueTask> handler)
    {
        _handlers.TryRemove(handler, out _);
    }

    public static async Task Invoke(T eventArgs)
    {
        var handlers = _handlers.ToArray();
        
        foreach (var handler in handlers)
        {
            await handler.Key.Invoke(eventArgs);
        }
    }
}
