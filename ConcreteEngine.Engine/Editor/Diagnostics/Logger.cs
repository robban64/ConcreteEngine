#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Editor.Utils;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Engine.Editor.Diagnostics;

public sealed record StringLogEvent(LogScope Scope, string Message, LogLevel Level = LogLevel.Info)
{
    public long Time { get; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
};

public static class Logger
{
    internal static Queue<LogEvent> LogQueue { get; } = new(16);
    private static bool _enabled = true;

    private static Action<StringLogEvent>? _logStringDel;

    public static bool IsAttached => _logStringDel != null;

    internal static void Attach(Action<StringLogEvent> logStringDel)
    {
        _logStringDel = logStringDel;
    }

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

    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _logStringDel?.Invoke(new StringLogEvent(scope, message, level));

    private static void Event(in LogEvent log)
    {
        if (!Enabled) return;
        if (LogQueue.Count > 100)
        {
            if (!IsAttached || !Enabled)
                LogQueue.Clear();
            else
                throw new InvalidOperationException("Logger queue overflow");
        }

        LogQueue.Enqueue(log);
    }

    public static void LogAssetObject(AssetObject asset, LogAction action, bool error = false) =>
        Event(new LogEvent(
            Id: (uint)asset.RawId.Value,
            Param0: 0,
            Param1: asset.IsCoreAsset ? 1 : 0,
            Gen: (ushort)asset.Generation,
            Flags: 0,
            Scope: LogScope.Assets,
            Topic: asset.Kind.ToLogTopic(),
            Action: action));
    /*
    public static LogEvent LogAssetSystem(LogTopic topic, LogAction action)
    {
        return new LogEvent(
            Id: (uint)asset.RawId.Value,
            Param0: 0,
            Param1: asset.IsCoreAsset ? 1 : 0,
            Gen: (ushort)asset.Generation,
            Flags:0 ,
            Scope: LogScope.Assets,
            Topic: LogTopic.Core,
            Action: action);
    }*/
}