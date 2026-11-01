using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Yvira.Vm;

/// <summary>
/// Representa uma tarefa assíncrona disparada via @spawn.funcao(...)
/// </summary>
public class YviraSpawnTask
{
    private readonly Task<object?> _internalTask;
    private readonly CancellationTokenSource _cts;

    public bool IsDone => _internalTask.IsCompleted;
    public bool IsFaulted => _internalTask.IsFaulted;
    public bool IsCanceled => _internalTask.IsCanceled;

    public YviraSpawnTask(Task<object?> internalTask, CancellationTokenSource cts)
    {
        _internalTask = internalTask;
        _cts = cts;
    }

    public object? Wait(double? timeoutSeconds = null)
    {
        try
        {
            if (timeoutSeconds.HasValue && timeoutSeconds.Value > 0)
            {
                int timeoutMs = (int)(timeoutSeconds.Value * 1000);
                if (_internalTask.Wait(timeoutMs))
                {
                    return _internalTask.Result;
                }
                throw new TimeoutException($"A tarefa assíncrona excedeu o tempo limite de {timeoutSeconds.Value} segundos.");
            }

            _internalTask.Wait();
            return _internalTask.Result;
        }
        catch (AggregateException ae)
        {
            throw ae.InnerException ?? ae;
        }
    }

    public void Cancel()
    {
        _cts.Cancel();
    }

    public YviraMap ToYviraObject()
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // task.wait([timeout])
        methods["wait"] = new HostFunction("task.wait", (interp, args) =>
        {
            double? timeout = args.Count > 0 && args[0] is double d ? d : null;
            return Wait(timeout);
        });

        // task.isDone()
        methods["isDone"] = new HostFunction("task.isDone", (interp, args) => IsDone);

        // task.cancel()
        methods["cancel"] = new HostFunction("task.cancel", (interp, args) =>
        {
            Cancel();
            return true;
        });

        return new YviraMap(methods);
    }
}

/// <summary>
/// Pool de Workers Multicore gerenciado pelo Yvira via @worker.pool(N)
/// </summary>
public class YviraWorkerPool : IDisposable
{
    private readonly int _concurrency;
    private readonly Interpreter _rootInterpreter;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    public int Concurrency => _concurrency;

    public YviraWorkerPool(int concurrency, Interpreter rootInterpreter)
    {
        _concurrency = concurrency > 0 ? concurrency : System.Environment.ProcessorCount;
        _rootInterpreter = rootInterpreter;
    }

    public YviraList Map(IReadOnlyList<object?> items, object? functionOrCallable)
    {
        if (_disposed) throw new ObjectDisposedException("O pool de workers já foi encerrado.");

        var results = new object?[items.Count];
        var partitioner = Partitioner.Create(0, items.Count);

        Parallel.ForEach(partitioner, new ParallelOptions
        {
            MaxDegreeOfParallelism = _concurrency,
            CancellationToken = _cts.Token
        }, range =>
        {
            // Cada partição/thread do pool opera com seu interpretador isolado
            var workerInterpreter = _rootInterpreter.CreateWorkerClone();

            for (int i = range.Item1; i < range.Item2; i++)
            {
                var item = items[i];
                object? res = null;

                if (functionOrCallable is YviraFunction fn)
                {
                    res = fn.Call(workerInterpreter, new List<object?> { item });
                }
                else if (functionOrCallable is HostFunction hf)
                {
                    res = hf.Call(workerInterpreter, new List<object?> { item });
                }
                else
                {
                    throw new Exception("O mapeamento requer uma função executável como segundo argumento.");
                }

                results[i] = res;
            }
        });

        return new YviraList(results);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    public YviraMap ToYviraObject()
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // pool.map(lista, funcao)
        methods["map"] = new HostFunction("pool.map", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("pool.map requer (lista, funcao).");

            IReadOnlyList<object?> items;
            if (args[0] is YviraList list)
            {
                items = list.Items;
            }
            else if (args[0] is YviraMatrix mat)
            {
                var listItems = new List<object?>();
                for (int r = 0; r < mat.Rows; r++)
                {
                    for (int c = 0; c < mat.Cols; c++)
                    {
                        listItems.Add(mat.Get(r, c));
                    }
                }
                items = listItems;
            }
            else if (args[0] is System.Collections.IEnumerable enumerable && args[0] is not string)
            {
                var listItems = new List<object?>();
                foreach (var el in enumerable) listItems.Add(el);
                items = listItems;
            }
            else
            {
                throw new Exception("pool.map requer uma lista ou matriz como primeiro argumento.");
            }

            return Map(items, args[1]);
        });

        // pool.concurrency
        methods["concurrency"] = (double)_concurrency;

        // pool.close()
        methods["close"] = new HostFunction("pool.close", (interp, args) =>
        {
            Dispose();
            return true;
        });

        return new YviraMap(methods);
    }
}
