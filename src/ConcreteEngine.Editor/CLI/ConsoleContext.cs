using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics;

namespace ConcreteEngine.Editor.CLI;

public sealed class ConsoleContext
{
    private const int MaxLogQueueSize = 512;
    private const int DrainPerTick = 6;
    
    private readonly Action<StringLogEvent> _addLogDel;
    
    private readonly Queue<StringLogEvent> _logQueue = new(256);
    
    public int HasLogs => _logQueue.Count;

    internal ConsoleContext(Action<StringLogEvent> addLogDel)
    {
        _addLogDel = addLogDel;
    }

    public void FlushLogQueue()
    {
        if(_logQueue.Count == 0) return;
        
        int drainLeft = DrainPerTick;
        while (drainLeft-- > 0 && _logQueue.TryDequeue(out var log))
        {
            _addLogDel.Invoke(log);
        }
    }

    public void AddLog(StringLogEvent? log)
    {
        if (log is null) return;
        _logQueue.Enqueue(log);
    }

    public void AddLog(string? log)
    {
        if (log is null) return;
        _logQueue.Enqueue(StringLogEvent.MakePlain(log));
    }

    public void AddMany(ReadOnlySpan<StringLogEvent> logs)
    {
        if (logs.Length == 0) return;
        _logQueue.EnsureCapacity(logs.Length);
        foreach (var log in logs)
            _logQueue.Enqueue(log);
    }
    
    public void AddMany(ReadOnlySpan<string> logs)
    {
        if (logs.Length == 0) return;
        _logQueue.EnsureCapacity(logs.Length);
        foreach (var log in logs)
            _logQueue.Enqueue(StringLogEvent.MakePlain(log));
    }


    private void Validate()
    {
        if(_logQueue.Count > MaxLogQueueSize) throw new InvalidOperationException("Log queue is full");
    }

}