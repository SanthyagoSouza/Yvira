using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Yvira.Vm;

public class YviraList : IEnumerable<object?>
{
    private readonly List<object?> _items;

    public int Count => _items.Count;
    public IReadOnlyList<object?> Items => _items;

    public YviraList()
    {
        _items = new List<object?>();
    }

    public YviraList(IEnumerable<object?> items)
    {
        _items = new List<object?>(items);
    }

    public void Add(object? item)
    {
        lock (_items)
        {
            _items.Add(item);
        }
    }

    public object? Get(int index)
    {
        int actualIndex = index < 0 ? _items.Count + index : index;
        if (actualIndex < 0 || actualIndex >= _items.Count)
        {
            throw new IndexOutOfRangeException($"�ndice fora dos limites da lista: {index} (tamanho: {_items.Count})");
        }
        return _items[actualIndex];
    }

    public void Set(int index, object? value)
    {
        int actualIndex = index < 0 ? _items.Count + index : index;
        if (actualIndex < 0 || actualIndex >= _items.Count)
        {
            throw new IndexOutOfRangeException($"�ndice fora dos limites da lista: {index} (tamanho: {_items.Count})");
        }
        _items[actualIndex] = value;
    }

    public object? Pop(int index = -1)
    {
        if (_items.Count == 0) throw new InvalidOperationException("N�o � poss�vel remover de uma lista vazia.");
        int actualIndex = index < 0 ? _items.Count + index : index;
        var item = _items[actualIndex];
        _items.RemoveAt(actualIndex);
        return item;
    }

    public void Insert(int index, object? value)
    {
        int actualIndex = index < 0 ? _items.Count + index : index;
        _items.Insert(actualIndex, value);
    }

    public void Clear() => _items.Clear();

    public bool Contains(object? item) => _items.Contains(item);

    public int IndexOf(object? item) => _items.IndexOf(item);

    public override string ToString()
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < _items.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var el = _items[i];
            if (el is string s) sb.Append($"\"{s}\"");
            else sb.Append(el?.ToString() ?? "null");
        }
        sb.Append("]");
        return sb.ToString();
    }

    public IEnumerator<object?> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class YviraMap : IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _entries = new();

    public int Count => _entries.Count;
    public IEnumerable<string> Keys => _entries.Keys;
    public IEnumerable<object?> Values => _entries.Values;

    public YviraMap() { }

    public YviraMap(IDictionary<string, object?> initial)
    {
        foreach (var kvp in initial)
        {
            _entries[kvp.Key] = kvp.Value;
        }
    }

    public object? Get(string key)
    {
        return _entries.TryGetValue(key, out var val) ? val : null;
    }

    public void Set(string key, object? value)
    {
        _entries[key] = value;
    }

    public bool HasKey(string key) => _entries.ContainsKey(key);

    public bool Remove(string key) => _entries.Remove(key);

    public void Clear() => _entries.Clear();
    public Dictionary<string, object?> ToDictionary() => new(_entries);

    public override string ToString()
    {
        var sb = new StringBuilder("{");
        int i = 0;
        foreach (var kvp in _entries)
        {
            if (i > 0) sb.Append(", ");
            sb.Append($"\"{kvp.Key}\": ");
            if (kvp.Value is string s) sb.Append($"\"{s}\"");
            else sb.Append(kvp.Value?.ToString() ?? "null");
            i++;
        }
        sb.Append("}");
        return sb.ToString();
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _entries.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
