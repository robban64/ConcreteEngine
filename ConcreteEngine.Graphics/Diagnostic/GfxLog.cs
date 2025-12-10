#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxLog
{
    public const int MaxQueueCapacity = 256;
    private static readonly Queue<LogEvent> Logs = new(128);
    private static readonly List<LogFilterWildcard> IgnoreFilter = new(4);

    public static bool Enabled { get; set; }

    public static int Count => Logs.Count;

    public static bool TryDrainLog(out LogEvent log) => Logs.TryDequeue(out log);

    internal static void Event(in LogEvent log)
    {
        if (!Enabled) return;
        if (Logs.Count >= MaxQueueCapacity)
        {
            Console.WriteLine("Log buffer full");
            return;
        }

        if (IgnoreFilter.Count > 0 && FilterLog(in log))
            return;

        Logs.Enqueue(log);
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

    private static bool FilterLog(in LogEvent log) => FilterLogIndex(log.Topic, log.Scope, log.Action, log.Level) >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FilterLogIndex(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
    {
        var packed = LogFilterWildcard.Pack((byte)topic, (byte)scope, (byte)action, (byte)level);
        return LogFilterWildcard.IndexAt(packed, IgnoreFilter);
    }

    // Utilities


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
}