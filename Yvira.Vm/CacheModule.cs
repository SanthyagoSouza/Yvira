using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Yvira.Vm;

public static class CacheModule
{
    private static readonly ConcurrentDictionary<string, MemoryCacheStore> _stores = new(StringComparer.OrdinalIgnoreCase);

    public static MemoryCacheStore GetDefaultStore() => GetStore("default");

    public static MemoryCacheStore GetStore(string name)
    {
        return _stores.GetOrAdd(string.IsNullOrWhiteSpace(name) ? "default" : name.Trim(), _ => new MemoryCacheStore());
    }

    public static YviraMap CreateModule(Interpreter? interpreter = null)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // cache.set(key, value, [ttlSeconds])
        methods["set"] = new HostFunction("cache.set", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null)
                throw new Exception("cache.set requer a chave e o valor. Exemplo: cache.set(\"chave\", valor, [ttlSegundos]);");

            string key = args[0]!.ToString()!;
            object? val = args[1];
            double? ttl = args.Count > 2 && args[2] != null ? Convert.ToDouble(args[2]) : null;

            GetDefaultStore().Set(key, val, ttl);
            return val;
        });

        // cache.get(key, [defaultValue])
        methods["get"] = new HostFunction("cache.get", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("cache.get requer a chave como primeiro argumento.");

            string key = args[0]!.ToString()!;
            var result = GetDefaultStore().Get(key);

            if (result == null && args.Count > 1)
            {
                return args[1];
            }
            return result;
        });

        // cache.has(key)
        methods["has"] = new HostFunction("cache.has", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return false;
            return GetDefaultStore().Has(args[0]!.ToString()!);
        });

        // cache.remove(key) ou cache.delete(key)
        methods["remove"] = new HostFunction("cache.remove", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return false;
            return GetDefaultStore().Remove(args[0]!.ToString()!);
        });
        methods["delete"] = methods["remove"];

        // cache.incr(key, [delta]) -> Incremento atômico de alta velocidade
        methods["incr"] = new HostFunction("cache.incr", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("cache.incr requer a chave.");

            string key = args[0]!.ToString()!;
            double delta = args.Count > 1 && args[1] != null ? Convert.ToDouble(args[1]) : 1.0;
            return GetDefaultStore().Increment(key, delta);
        });

        // cache.decr(key, [delta]) -> Decremento atômico
        methods["decr"] = new HostFunction("cache.decr", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("cache.decr requer a chave.");

            string key = args[0]!.ToString()!;
            double delta = args.Count > 1 && args[1] != null ? Convert.ToDouble(args[1]) : 1.0;
            return GetDefaultStore().Increment(key, -delta);
        });

        // cache.clear()
        methods["clear"] = new HostFunction("cache.clear", (interp, args) =>
        {
            GetDefaultStore().Clear();
            return null;
        });

        // cache.size() ou cache.count()
        methods["size"] = new HostFunction("cache.size", (interp, args) =>
        {
            return (double)GetDefaultStore().Count;
        });
        methods["count"] = methods["size"];

        // cache.all() -> retorna YviraMap com todas as entradas
        methods["all"] = new HostFunction("cache.all", (interp, args) =>
        {
            return GetDefaultStore().GetAll();
        });

        // cache.store(name) -> cria ou obtém um bucket/store isolado
        methods["store"] = new HostFunction("cache.store", (interp, args) =>
        {
            string storeName = args.Count > 0 && args[0] != null ? args[0]!.ToString()! : "default";
            var store = GetStore(storeName);
            return CreateStoreMap(store);
        });

        return new YviraMap(methods);
    }

    private static YviraMap CreateStoreMap(MemoryCacheStore store)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        methods["set"] = new HostFunction("store.set", (interp, args) =>
        {
            if (args.Count < 2 || args[0] == null) throw new Exception("store.set requer chave e valor.");
            string key = args[0]!.ToString()!;
            object? val = args[1];
            double? ttl = args.Count > 2 && args[2] != null ? Convert.ToDouble(args[2]) : null;
            store.Set(key, val, ttl);
            return val;
        });

        methods["get"] = new HostFunction("store.get", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return null;
            string key = args[0]!.ToString()!;
            var result = store.Get(key);
            if (result == null && args.Count > 1) return args[1];
            return result;
        });

        methods["has"] = new HostFunction("store.has", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return false;
            return store.Has(args[0]!.ToString()!);
        });

        methods["remove"] = new HostFunction("store.remove", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return false;
            return store.Remove(args[0]!.ToString()!);
        });
        methods["delete"] = methods["remove"];

        methods["incr"] = new HostFunction("store.incr", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) throw new Exception("store.incr requer a chave.");
            string key = args[0]!.ToString()!;
            double delta = args.Count > 1 && args[1] != null ? Convert.ToDouble(args[1]) : 1.0;
            return store.Increment(key, delta);
        });

        methods["decr"] = new HostFunction("store.decr", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) throw new Exception("store.decr requer a chave.");
            string key = args[0]!.ToString()!;
            double delta = args.Count > 1 && args[1] != null ? Convert.ToDouble(args[1]) : 1.0;
            return store.Increment(key, -delta);
        });

        methods["clear"] = new HostFunction("store.clear", (interp, args) =>
        {
            store.Clear();
            return null;
        });

        methods["size"] = new HostFunction("store.size", (interp, args) => (double)store.Count);
        methods["count"] = methods["size"];
        methods["all"] = new HostFunction("store.all", (interp, args) => store.GetAll());

        return new YviraMap(methods);
    }
}
