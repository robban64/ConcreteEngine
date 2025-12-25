using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Editor.Utils;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Diagnostics;

public static class Logger
{
    public const int MaxQueueCapacity = 256;
    private static readonly Queue<LogEvent> Logs = new(128);
    private static readonly List<StringLogEvent> StringLogsBuffer = new(16);
    private static readonly List<LogFilterWildcard> IgnoreFilter = new(4);

    private static CliContext _cliContext = null!;

    public static bool Enabled { get; set; }

    public static int Count => Logs.Count;
    public static bool IsAttached => _cliContext != null!;
    public static bool HasPendingStringLogs => StringLogsBuffer.Count > 0;

    public static bool TryDrainLog(out LogEvent log) => Logs.TryDequeue(out log);

    internal static void Attach()
    {
        if(_cliContext is not null) throw new InvalidOperationException("Already attached");
        
        var context = EditorCli.Context;
        _cliContext = context ?? throw new InvalidOperationException("CliContext is null");
    }

    internal static void FlushStringLogs()
    {
        if (_cliContext is null) throw new InvalidOperationException();
        foreach (var log in StringLogsBuffer) _cliContext.AddLog(log);
        StringLogsBuffer.Clear();
        StringLogsBuffer.TrimExcess();
    }


    private static void Event(in LogEvent log)
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

    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info)
    {
        var log = new StringLogEvent(scope, message, level);
        if (IsAttached )
        {
            _cliContext.AddLog(new StringLogEvent(scope, message, level));
            return;
        }

        if (StringLogsBuffer.Count > MaxQueueCapacity) throw new InvalidOperationException("String log buffer full");
        StringLogsBuffer.Add(log);
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



    public static void LogAssetObject(AssetObject asset, LogAction action, bool error = false) =>
        Event(new LogEvent(
            id: (uint)asset.RawId.Value,
            param0: 0,
            param1: asset.IsCoreAsset ? 1 : 0,
            gen: (ushort)asset.Generation,
            flags: 0,
            scope: LogScope.Assets,
            topic: asset.Kind.ToLogTopic(),
            action: action));


    private static bool FilterLog(in LogEvent log) => FilterLogIndex(log.Topic, log.Scope, log.Action, log.Level) >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FilterLogIndex(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
    {
        var packed = LogFilterWildcard.Pack((byte)topic, (byte)scope, (byte)action, (byte)level);
        return LogFilterWildcard.IndexAt(packed, IgnoreFilter);
    }

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