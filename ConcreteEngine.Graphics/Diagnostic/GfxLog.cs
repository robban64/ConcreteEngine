#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxLog
{
    public static Queue<LogEvent> LogQueue { get; } = new(16);

    private static readonly List<LogFilterWildcard> IgnoreFilter = new(4);

    private static bool _enabled = false;

    public static bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            LogQueue.Clear();
            _enabled = value;
        }
    }


    private static void Event(in LogEvent log)
    {
        if (!Enabled) return;
        if (LogQueue.Count > 100)
        {
            if (!Enabled)
                LogQueue.Clear();
            else
                throw new InvalidOperationException("Logger queue overflow");
        }

        if (FilterLog(in log))
            return;

        LogQueue.Enqueue(log);
    }


    public static void ToggleLog(bool enabled, LogTopic topic = 0, LogScope scope = 0, LogAction action = 0,
        LogLevel level = 0)
    {
        var rule = new LogFilterWildcard(topic, scope, action, level);
        var idx = FilterLogIndex(topic, scope, action, level);

        if (enabled && idx >= 0)
            IgnoreFilter.RemoveAt(idx);
        else if (!enabled && idx == -1)
            IgnoreFilter.Add(rule);
    }


    private static LogEvent LogGfx(int id, int slot, ushort gen, ushort flags, bool alive, LogTopic topic,
        LogAction action) =>
        new((uint)id, param0: slot, param1: alive ? 1 : 0, gen: gen, flags: flags, scope: LogScope.Gfx, topic: topic,
            action: action);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogGfxStore(int id, GfxHandle h, LogTopic topic, LogAction action, ushort flags = 0) =>
        Event(LogGfx(id, h.Slot, h.Gen, flags, h.IsValid, topic, action));

    //
    private static LogEvent LogBk(uint handle, int slot, ushort flags, bool alive, LogTopic topic, LogAction action) =>
        new(handle, slot, alive ? 1 : 0, flags: flags, scope: LogScope.Backend, topic: topic, action: action);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogBkStore(uint handle, int slot, LogTopic topic, LogAction action, ushort flags = 0) =>
        Event(LogBk(handle, slot, flags, handle > 0, topic, action));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogBackend(uint handle, GfxHandle h, LogTopic topic, LogAction action, ushort flags = 0) =>
        Event(LogBk(handle, h.Slot, flags, h.IsValid, topic, action));


    private static bool FilterLog(in LogEvent log) =>
        FilterLogIndex(log.Topic, log.Scope, log.Action, log.Level) >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FilterLogIndex(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
    {
        var packed = LogFilterWildcard.Pack((byte)topic, (byte)scope, (byte)action, (byte)level);
        return LogFilterWildcard.IndexAt(packed, IgnoreFilter);
    }
}