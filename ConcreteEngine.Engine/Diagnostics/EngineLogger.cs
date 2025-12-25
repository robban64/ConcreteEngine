using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Editor.Utils;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Diagnostics;

public sealed class EngineLogger
{
    internal readonly StructLogParser StructParser;
    private readonly List<LogFilterWildcard> _ignoreFilter = new(4);
    private readonly ConsoleContext _consoleContext;

    internal EngineLogger(ConsoleContext consoleContext)
    {
        _consoleContext = consoleContext;
        StructParser = new StructLogParser();
    }

    public bool Enabled { get; set; }
    public bool IsAttached => _consoleContext != null!;
    
    private void LogValueEvent(in LogEvent log)
    {
        if (!Enabled) return;
        if (_ignoreFilter.Count > 0 && FilterLog(in log)) return;
        _consoleContext.AddLog(StructParser.ToStringLog(in log));
    }

    public void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info)
    {
        _consoleContext.AddLog(new StringLogEvent(scope, message, level));
    }
    
    public void LogMany(ReadOnlySpan<StringLogEvent> logs)
    {
        _consoleContext.AddMany(logs);
    }
    
    public void LogAssetObject(AssetObject asset, LogAction action, bool error = false) =>
        LogValueEvent(new LogEvent(
            id: (uint)asset.RawId.Value,
            param0: 0,
            param1: asset.IsCoreAsset ? 1 : 0,
            gen: (ushort)asset.Generation,
            flags: 0,
            scope: LogScope.Assets,
            topic: asset.Kind.ToLogTopic(),
            action: action));

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




    private bool FilterLog(in LogEvent log) => FilterLogIndex(log.Topic, log.Scope, log.Action, log.Level) >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FilterLogIndex(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
    {
        var packed = LogFilterWildcard.Pack((byte)topic, (byte)scope, (byte)action, (byte)level);
        return LogFilterWildcard.IndexAt(packed, CollectionsMarshal.AsSpan(_ignoreFilter));
    }

    /*
    public  LogEvent LogAssetSystem(LogTopic topic, LogAction action)
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