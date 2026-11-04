using System;
using System.Collections.Generic;
using System.Linq;

namespace Yvira.Vm;

public class YviraEnum
{
    public string Name { get; }
    private readonly Dictionary<string, object?> _members = new(StringComparer.OrdinalIgnoreCase);

    public YviraEnum(string name)
    {
        Name = name;
    }

    public void AddMember(string name, object? value)
    {
        _members[name] = value;
    }

    public object? Get(string name)
    {
        if (_members.TryGetValue(name, out var val))
        {
            return val;
        }

        if (name.Equals("keys", StringComparison.OrdinalIgnoreCase) || name.Equals("names", StringComparison.OrdinalIgnoreCase))
        {
            return new YviraList(_members.Keys.Select(k => (object?)k));
        }

        if (name.Equals("values", StringComparison.OrdinalIgnoreCase))
        {
            return new YviraList(_members.Values);
        }

        throw new YviraRuntimeException($"Membro '{name}' não existe no enum '{Name}'.");
    }

    public bool HasMember(string name) => _members.ContainsKey(name);

    public override string ToString()
    {
        var membersDesc = string.Join(", ", _members.Select(kv => $"{kv.Key} = {kv.Value}"));
        return $"<enum {Name} {{ {membersDesc} }}>";
    }
}
