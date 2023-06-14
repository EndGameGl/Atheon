using System.Collections.Concurrent;

namespace Atheon.Models.Collections;

public class UniqueConcurrentQueue<T>
{
    private SemaphoreSlim _semaphoreSlim;
    private ConcurrentHashSet<T> _hashSet;
    private ConcurrentQueue<T> _queue;

    public UniqueConcurrentQueue()
    {
        _semaphoreSlim = new(1, 1);
        _hashSet = new ConcurrentHashSet<T>();
        _queue = new ConcurrentQueue<T>();
    }

    public void EnqueueRange(IEnumerable<T> range)
    {
        _semaphoreSlim.Wait();

        try
        {
            foreach (var item in range)
            {
                _queue.Enqueue(item);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public void Enqueue(T item)
    {
        _semaphoreSlim.Wait();

        try
        {
            _queue.Enqueue(item);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public IEnumerable<T> DequeueUpTo(int amount)
    {
        _semaphoreSlim.Wait();

        try
        {
            int itemsTaken = 0;
            while (itemsTaken < amount && _queue.TryDequeue(out var item))
            {
                amount++;
                _hashSet.Remove(item);
                yield return item;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public int Count
    {
        get
        {
            _semaphoreSlim.Wait();

            try
            {
                return _queue.Count;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
