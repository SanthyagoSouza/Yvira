using System;
using System.Collections.Generic;

namespace Yvira.Vm;

public class Environment
{
    private readonly Dictionary<string, object?> _values = new();
    private readonly HashSet<string> _constants = new();
    private readonly Environment? _parent;

    public Environment(Environment? parent = null)
    {
        _parent = parent;
    }

    // Define uma nova variÃ¡vel no escopo atual
    public void Define(string name, object? value, bool isConst = false)
    {
        if (_values.ContainsKey(name))
        {
            throw new Exception($"Erro de ExecuÃ§Ã£o: VariÃ¡vel '{name}' jÃ¡ foi declarada neste escopo.");
        }

        _values[name] = value;
        if (isConst)
        {
            _constants.Add(name);
        }
    }

    public bool Exists(string name)
    {
        if (_values.ContainsKey(name)) return true;
        if (_parent != null) return _parent.Exists(name);
        return false;
    }

    public object? Get(string name)
    {
        if (_values.TryGetValue(name, out var val))
        {
            return val;
        }

        if (_parent != null)
        {
            return _parent.Get(name);
        }

        throw new Exception($"Erro de ExecuÃ§Ã£o: VariÃ¡vel '{name}' nÃ£o foi declarada.");
    }

    // Atribui novo valor a uma variÃ¡vel existente
    public void Assign(string name, object? value)
    {
        if (_values.ContainsKey(name))
        {
            if (_constants.Contains(name))
            {
                throw new Exception($"Erro de ExecuÃ§Ã£o: NÃ£o Ã© permitido reatribuir valor Ã  constante '{name}'.");
            }

            _values[name] = value;
            return;
        }

        if (_parent != null)
        {
            _parent.Assign(name, value);
            return;
        }

        throw new Exception($"Erro de ExecuÃ§Ã£o: VariÃ¡vel '{name}' nÃ£o existe para atribuiÃ§Ã£o.");
    }
    public IReadOnlyDictionary<string, object?> GetAllLocal()
    {
        return _values;
    }

    public bool IsDefinedLocally(string name)
    {
        return _values.ContainsKey(name);
    }
}