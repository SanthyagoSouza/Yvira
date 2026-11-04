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

    public bool IsSubclassOf(YviraClass targetClass)
    {
        if (this == targetClass || Name.Equals(targetClass.Name, StringComparison.OrdinalIgnoreCase))
            return true;
        return SuperClass != null && SuperClass.IsSubclassOf(targetClass);
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
        if (name.Equals("super", StringComparison.OrdinalIgnoreCase) && Class.SuperClass != null)
        {
            return new YviraSuperInstance(this, Class.SuperClass);
        }

        if (name.Equals("__class__", StringComparison.OrdinalIgnoreCase) || name.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            return Class;
        }

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

public class YviraSuperInstance
{
    public YviraInstance Instance { get; }
    public YviraClass SuperClass { get; }

    public YviraSuperInstance(YviraInstance instance, YviraClass superClass)
    {
        Instance = instance;
        SuperClass = superClass;
    }

    public object? Get(string name)
    {
        var method = SuperClass.FindMethod(name);
        if (method != null)
        {
            return method.Bind(Instance);
        }

        return Instance.Get(name);
    }

    public override string ToString() => $"<super {SuperClass.Name} de {Instance.Class.Name}>";
}
