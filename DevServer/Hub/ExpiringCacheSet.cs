// <copyright file="ExpiringCacheSet.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Concurrent;

namespace DevServer.Hub;

/// <summary>
/// Represents a set of key-value pairs that are removed after a specified expiration time.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the cache.</typeparam>
/// <typeparam name="TValue">The type of the values in the cache.</typeparam>
public class ExpiringCacheSet<TKey, TValue>
    where TKey : notnull
{
    private readonly TimeSpan _expiration;
    private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime Timestamp)> _cache;
    private readonly Action<TKey, TValue>? _onItemRemoved;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpiringCacheSet{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="expiration">The expiration time for items in the cache.</param>
    /// <param name="onItemRemoved">An optional action to perform when an item is removed from the cache.</param>
    public ExpiringCacheSet(TimeSpan expiration, Action<TKey, TValue>? onItemRemoved = null)
    {
        _expiration = expiration;
        _onItemRemoved = onItemRemoved;
        _cache = new ConcurrentDictionary<TKey, (TValue Value, DateTime Timestamp)>();
        RunPeriodic(Cleanup, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Attempts to retrieve a value from the cache.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">The value retrieved from the cache.</param>
    /// <returns><see langword="true"/> if the value was retrieved; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(TKey key, out TValue value)
    {
        var exists = _cache.TryGetValue(key, out var cached);
        value = exists ? cached.Value : default!;
        return exists;
    }

    /// <summary>
    /// Gets all entries in the cache that have not expired.
    /// </summary>
    /// <returns>A dictionary of all entries in the cache that have not expired.</returns>
    public IDictionary<TKey, TValue> GetAllEntries()
    {
        var currentTime = DateTime.UtcNow;
        return _cache.Where(kv => currentTime - kv.Value.Timestamp < _expiration)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Value);
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <param name="key">The key of the value to set.</param>
    /// <param name="value">The value to set in the cache.</param>
    public void Set(TKey key, TValue value)
    {
        _cache[key] = (value, DateTime.UtcNow);
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

    private void Cleanup()
    {
        var expired = DateTime.UtcNow - _expiration;
        foreach (var (key, value) in _cache)
        {
            if (value.Timestamp < expired)
            {
                if (_cache.TryRemove(key, out var removedValue))
                {
                    _onItemRemoved?.Invoke(key, removedValue.Value);
                }
            }
        }
    }
}
