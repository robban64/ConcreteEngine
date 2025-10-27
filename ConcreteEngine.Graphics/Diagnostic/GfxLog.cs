#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Resources;

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
            InvalidOpThrower.ThrowIf(LogQueue.Count > 512);
            Debug.Assert(false);
            return;
        }

        if (FilterLog(in log))
            return;

        LogQueue.Enqueue(log);
    }


    public static void ToggleLog(bool enabled, LogTopic topic = 0, LogScope scope = 0, LogAction action = 0,
        LogLevel level = 0)
    {
        var rule = new LogFilterWildcard(topic, scope, action, level);

        if (enabled)
        {
            for (var i = 0; i < IgnoreFilter.Count; i++)
            {
                if (IgnoreFilter[i] != rule) continue;
                IgnoreFilter.RemoveAt(i);
                return;
            }

            return;
        }

        foreach (var t in IgnoreFilter)
        {
            if (t == rule) return;
        }

        IgnoreFilter.Add(rule);
    }


    private static LogEvent LogGfx(int id, int slot, ushort gen, ushort flags, bool alive, LogTopic topic,
        LogAction action) =>
        new((uint)id, Param0: slot, Param1: alive ? 1 : 0, Gen: gen, Flags: flags, Scope: LogScope.Gfx, Topic: topic,
            Action: action);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogGfxStore<TId>(TId id, GfxHandle h, LogTopic topic, LogAction action, ushort flags = 0)
        where TId : unmanaged, IResourceId => Event(LogGfx(id.Value, h.Slot, h.Gen, flags, h.IsValid, topic, action));

    //
    private static LogEvent LogBk(uint handle, int slot, ushort flags, bool alive, LogTopic topic, LogAction action)
        => new(handle, slot, alive ? 1 : 0, Flags: flags, Scope: LogScope.Backend, Topic: topic, Action: action);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogBkStore<THandle>(BkHandle<THandle> handle, int slot, LogTopic topic, LogAction action,
        ushort flags = 0)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle> =>
        Event(LogBk(handle, (int)slot, flags, true, topic, action));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogBackend(uint handle, GfxHandle h, LogTopic topic, LogAction action, ushort flags = 0) =>
        Event(LogBk(handle, h.Slot, flags, h.IsValid, topic, action));


    private static bool FilterLog(in LogEvent log)
    {
        foreach (var it in IgnoreFilter)
        {
            var validKind = it.Topic == 0 || it.Topic == (byte)log.Topic;
            var validLayer = it.Scope == 0 || it.Scope == (byte)log.Scope;
            var validAction = it.Action == 0 || it.Action == (byte)log.Action;
            var validSource = it.Level == 0 || it.Level == (byte)log.Level;

            if (validKind && validLayer && validSource && validAction) return true;
        }

        return false;
    }

/*
    public static LogEvent MakeResourceDispose(in DeleteResourceCommand cmd)
    {
        var handle = (int)cmd.BackendHandle.Value;
        var h = cmd.Handle;
        return new LogEvent(handle, h.Slot, h.Gen, h.Kind, GfxLogLayer.Backend, GfxLogSource.Resource,
            GfxLogAction.Dispose);
    }
*/
}