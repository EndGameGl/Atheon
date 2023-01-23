namespace Atheon.Models.Collections;

public class ConcurrentHashSet<T>
{
    private HashSet<T> _hashSet;
    private SemaphoreSlim _semaphoreSlim;

    public ConcurrentHashSet()
    {
        _hashSet = new HashSet<T>();
        _semaphoreSlim = new(1, 1);
    }

    public bool Add(T item)
    {
        _semaphoreSlim.Wait();
        try
        {
            return _hashSet.Add(item);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public bool Remove(T item)
    {
        _semaphoreSlim.Wait();
        try
        {
            return _hashSet.Remove(item);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}
