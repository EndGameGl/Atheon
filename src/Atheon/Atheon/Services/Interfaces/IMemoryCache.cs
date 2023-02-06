using Atheon.Services.Caching;

namespace Atheon.Services.Interfaces;

public interface IMemoryCache
{
    ValueTask<T?> GetOrAddAsync<T>(string key, Func<ValueTask<T?>> entryFactory, TimeSpan timeToStore, CacheExpirationType expirationType);
    void CleanExpiredEntries();
}
