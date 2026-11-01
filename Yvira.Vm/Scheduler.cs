using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Yvira.Vm;

public class ScheduledTask : IDisposable
{
    public string Name { get; }
    public double IntervalSeconds { get; }
    public object Callable { get; }
    public object?[] Arguments { get; }
    public System.Threading.Timer? Timer { get; set; }
    public bool IsRunning { get; set; }

    public ScheduledTask(string name, double intervalSeconds, object callable, object?[] arguments)
    {
        Name = name;
        IntervalSeconds = intervalSeconds;
        Callable = callable;
        Arguments = arguments;
        IsRunning = true;
    }

    public void Dispose()
    {
        IsRunning = false;
        Timer?.Dispose();
        Timer = null;
    }
}

public class Scheduler : IDisposable
{
    private readonly ConcurrentDictionary<string, ScheduledTask> _tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public bool HasActiveTasks => !_tasks.IsEmpty;

    public void Schedule(string name, double intervalSeconds, object callable, object?[] arguments, Action<ScheduledTask> runner)
    {
        // Se ja existir uma tarefa com o mesmo nome, cancela a anterior
        Stop(name);

        var task = new ScheduledTask(name, intervalSeconds, callable, arguments);
        int dueTimeMs = (int)(intervalSeconds * 1000);
        int periodMs = (int)(intervalSeconds * 1000);

        task.Timer = new System.Threading.Timer(_ =>
        {
            if (task.IsRunning)
            {
                runner(task);
            }
        }, null, dueTimeMs, periodMs);

        _tasks[name] = task;
    }

    public bool Stop(string name)
    {
        if (_tasks.TryRemove(name, out var task))
        {
            task.Dispose();
            return true;
        }
        return false;
    }

    public void StopAll()
    {
        foreach (var kvp in _tasks)
        {
            kvp.Value.Dispose();
        }
        _tasks.Clear();
    }

    public void Dispose()
    {
        StopAll();
    }
}
