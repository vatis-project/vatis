using System.Collections.Concurrent;

namespace DevServer.Hub;

public class ExpiringCacheSet<TKey, TValue> where TKey : notnull
{
    private readonly TimeSpan _expiration;
    private readonly ConcurrentDictionary<TKey, (TValue value, DateTime timestamp)> _cache;
    private readonly Action<TKey, TValue>? _onItemRemoved;

    public ExpiringCacheSet(TimeSpan expiration, Action<TKey, TValue>? onItemRemoved = null)
    {
        _expiration = expiration;
        _onItemRemoved = onItemRemoved;
        _cache = new ConcurrentDictionary<TKey, (TValue value, DateTime timestamp)>();
        RunPeriodic(Cleanup, TimeSpan.FromSeconds(30));
    }

    public bool TryGet(TKey key, out TValue value)
    {
        var exists = _cache.TryGetValue(key, out var cached);
        value = exists ? cached.value : default!;
        return exists;
    }

    public IDictionary<TKey, TValue> GetAllEntries()
    {
        var currentTime = DateTime.UtcNow;
        return _cache.Where(kv => currentTime - kv.Value.timestamp < _expiration)
            .ToDictionary(kv => kv.Key, kv => kv.Value.value);
    }

    public void Set(TKey key, TValue value)
    {
        _cache[key] = (value, DateTime.UtcNow);
    }

    private void Cleanup()
    {
        var expired = DateTime.UtcNow - _expiration;
        foreach (var (key, value) in _cache)
        {
            if (value.timestamp < expired)
            {
                if (_cache.TryRemove(key, out var removedValue))
                {
                    _onItemRemoved?.Invoke(key, removedValue.value);
                }
            }
        }
    }

    private static void RunPeriodic(Action action, TimeSpan interval)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    action();
                }
                catch
                {
                    // ignored
                }

                await Task.Delay(interval);
            }
            // ReSharper disable once FunctionNeverReturns
        });
    }
}
