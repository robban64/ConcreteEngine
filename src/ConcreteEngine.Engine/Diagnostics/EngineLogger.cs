using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Editor.CLI;

namespace ConcreteEngine.Engine.Diagnostics;

public sealed class EngineLogger
{
    private readonly List<LogFilterWildcard> _ignoreFilter = new(4);
    private readonly ConsoleContext _consoleContext;

    internal EngineLogger(ConsoleContext consoleContext)
    {
        _consoleContext = consoleContext;
    }

    public bool Enabled { get; set; }
    public bool IsAttached => _consoleContext != null!;


    public void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info)
    {
        _consoleContext.AddLog(new StringLogEvent(scope, message, level));
    }

    public void LogMany(ReadOnlySpan<StringLogEvent> logs)
    {
        _consoleContext.AddMany(logs);
    }

    private bool FilterLog(in LogEvent log) => FilterLogIndex(log.Topic, log.Scope, log.Action, log.Level) >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FilterLogIndex(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
    {
        var packed = LogFilterWildcard.Pack((byte)topic, (byte)scope, (byte)action, (byte)level);
        return LogFilterWildcard.IndexAt(packed, _ignoreFilter);
    }


    public void ToggleLog(bool enabled, LogTopic topic = 0, LogScope scope = 0, LogAction action = 0,
        LogLevel level = 0)
    {
        var rule = new LogFilterWildcard(topic, scope, action, level);
        var idx = FilterLogIndex(topic, scope, action, level);

        if (enabled && idx >= 0)
            _ignoreFilter.RemoveAt(idx);
        else if (!enabled && idx == -1)
            _ignoreFilter.Add(rule);
    }
}