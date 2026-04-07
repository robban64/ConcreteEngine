using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Gateway.Diagnostics;

public static class Logger
{
    private static Action<StringLogEvent> _boundLogger = static (log) => TempLog(log);

    private static List<StringLogEvent> _tempLogs = new(32);

    public static void Setup()
    {
        foreach (var log in _tempLogs) ConsoleGateway.Log(log);
        _tempLogs = null!;

        _boundLogger = static log => ConsoleGateway.Log(log);

        SetupGfxLogger();
    }

    public static void ToggleGfxLog(bool enabled) => GfxLog.Enabled = enabled;

    private static unsafe void SetupGfxLogger()
    {
        if (GfxLog.IsBound) throw new InvalidOperationException("GfxLogger is already active");

        GfxLog.Setup(&ConsoleGateway.LogStruct);
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
        GfxLog.Enabled = true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _boundLogger(new StringLogEvent(scope, message, level));


    private static void TempLog(StringLogEvent log)
    {
        if (_tempLogs is null) throw new InvalidOperationException(nameof(_tempLogs));
        Console.WriteLine(log.Message);
/*
        _tempLogs.Add(new StringLogEvent(scope, message, level));
        if (_tempLogs.Count > 32)
            Console.Error.WriteLine("Pre Log Buffer count high");

        if (_tempLogs.Count > 512) throw new InvalidOperationException("Pre Log Buffer count high");*/
    }
}