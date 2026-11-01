using System;
using System.Collections.Generic;
using System.Linq;

namespace Yvira.Vm;

public class YviraClass
{
    public string Name { get; }
    public YviraClass? SuperClass { get; }
    public Dictionary<string, YviraFunction> Methods { get; }
    public Dictionary<string, YviraFunction> StaticMethods { get; }

    public YviraClass(string name, YviraClass? superClass, Dictionary<string, YviraFunction> methods, Dictionary<string, YviraFunction> staticMethods)
    {
        Name = name;
        SuperClass = superClass;
        Methods = methods;
        StaticMethods = staticMethods;
    }

    public YviraFunction? FindMethod(string name)
    {
        if (Methods.TryGetValue(name, out var method)) return method;
        if (SuperClass != null) return SuperClass.FindMethod(name);
        return null;
    }

    public YviraFunction? FindStaticMethod(string name)
    {
        if (StaticMethods.TryGetValue(name, out var method)) return method;
        if (SuperClass != null) return SuperClass.FindStaticMethod(name);
        return null;
    }

    public YviraInstance Instantiate(Interpreter interpreter, IReadOnlyList<object?> arguments)
    {
        var instance = new YviraInstance(this);

        // Se houver construtor chamado 'constructor' ou 'init', executa-o
        var initMethod = FindMethod("constructor") ?? FindMethod("init");
        if (initMethod != null)
        {
            initMethod.Bind(instance).Call(interpreter, arguments);
        }

        return instance;
    }

    public override string ToString() => $"<classe {Name}>";
}

public class YviraInstance
{
    public YviraClass Class { get; }
    public Dictionary<string, object?> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);

    public YviraInstance(YviraClass @class)
    {
        Class = @class;
    }

    public object? Get(string name)
    {
        if (Fields.TryGetValue(name, out var val))
        {
            return val;
        }

        var method = Class.FindMethod(name);
        if (method != null)
        {
            return method.Bind(this);
        }

        throw new YviraRuntimeException($"Propriedade ou método indefinido '{name}' na instância de '{Class.Name}'.");
    }

    public void Set(string name, object? value)
    {
        Fields[name] = value;
    }

    public override string ToString()
    {
        var fieldsDesc = string.Join(", ", Fields.Select(kv => $"{kv.Key}: {kv.Value}"));
        return $"<{Class.Name} {{ {fieldsDesc} }}>";
    }
}
