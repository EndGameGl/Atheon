using Atheon.Services.Interfaces;
using System.Collections.Concurrent;

namespace Atheon.Services.Caching;

public class MemoryCacheImplementation : IMemoryCache
{
    private readonly ConcurrentDictionary<string, MemoryCacheEntry> _storage;
    private readonly ILogger<MemoryCacheImplementation> _logger;

    public MemoryCacheImplementation(
        ILogger<MemoryCacheImplementation> logger)
    {
        _storage = new ConcurrentDictionary<string, MemoryCacheEntry>();
        _logger = logger;
    }

    public void CleanExpiredEntries()
    {
        var currentDate = DateTime.UtcNow;
        var expiredEntries = _storage.Where(x => x.Value.DidExpire(ref currentDate)).ToList();
        foreach (var entry in expiredEntries)
        {
            _storage.Remove(entry.Key, out _);
        }
    }

    public async ValueTask<T?> GetOrAddAsync<T>(
        string key,
        Func<ValueTask<T>> entryFactory,
        TimeSpan timeToStore,
        CacheExpirationType expirationType)
    {
        var currentDate = DateTime.UtcNow;

        if (_storage.TryGetValue(key, out var entry))
        {
            if (entry.DidExpire(ref currentDate))
            {
                try
                {
                    var updatedValue = await entryFactory();
                    entry.UpdateValue(updatedValue);
                    return updatedValue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get value for memory cache with key {Key}", key);
                    throw;
                }

            }
            if (expirationType == CacheExpirationType.Sliding)
            {
                entry.DateStored = currentDate;
            }
            return (T?)entry.Value;
        }

        try
        {
            var newValue = await entryFactory();
            var newCacheEntry = new MemoryCacheEntry()
            {
                Value = newValue,
                DateStored = currentDate,
                TimeToStore = timeToStore
            };
            _storage.TryAdd(key, newCacheEntry);
            return newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get value for memory cache with key {Key}", key);
            throw;
        }
    }
}
