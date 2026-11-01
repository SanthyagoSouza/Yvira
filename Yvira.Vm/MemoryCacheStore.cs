using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Yvira.Vm;

public class CacheEntry
{
    public object? Value { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class MemoryCacheStore
{
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, object? value, double? ttlSeconds = null)
    {
        DateTime? expiresAt = ttlSeconds.HasValue && ttlSeconds.Value > 0
            ? DateTime.UtcNow.AddSeconds(ttlSeconds.Value)
            : null;

        _store[key] = new CacheEntry { Value = value, ExpiresAt = expiresAt };
    }

    public object? Get(string key)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _store.TryRemove(key, out _);
                return null;
            }
            return entry.Value;
        }
        return null;
    }

    public bool Has(string key)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _store.TryRemove(key, out _);
                return false;
            }
            return true;
        }
        return false;
    }

    public bool Remove(string key) => _store.TryRemove(key, out _);

    public void Clear() => _store.Clear();

    public double Increment(string key, double delta = 1.0)
    {
        while (true)
        {
            if (_store.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    _store.TryRemove(key, out _);
                    var newEntry = new CacheEntry { Value = delta, ExpiresAt = null };
                    if (_store.TryAdd(key, newEntry)) return delta;
                    continue;
                }

                double cur = entry.Value != null ? Convert.ToDouble(entry.Value) : 0.0;
                double updated = cur + delta;
                var updatedEntry = new CacheEntry { Value = updated, ExpiresAt = entry.ExpiresAt };
                if (_store.TryUpdate(key, updatedEntry, entry))
                {
                    return updated;
                }
            }
            else
            {
                var newEntry = new CacheEntry { Value = delta, ExpiresAt = null };
                if (_store.TryAdd(key, newEntry)) return delta;
            }
        }
    }

    public int Count
    {
        get
        {
            int c = 0;
            var now = DateTime.UtcNow;
            foreach (var kvp in _store)
            {
                if (!kvp.Value.ExpiresAt.HasValue || kvp.Value.ExpiresAt.Value > now)
                    c++;
            }
            return c;
        }
    }

    public YviraMap GetAll()
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        foreach (var kvp in _store)
        {
            if (!kvp.Value.ExpiresAt.HasValue || kvp.Value.ExpiresAt.Value > now)
            {
                dict[kvp.Key] = kvp.Value.Value;
            }
        }
        return new YviraMap(dict);
    }
}
