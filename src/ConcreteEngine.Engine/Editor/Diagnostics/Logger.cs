using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Editor.Diagnostics;

public static class Logger
{
    private static LoggerDel _boundLogger = static (scope, message, level) => TempLog(scope, message, level);

    private static List<StringLogEvent> _tempLogs = new(32);

    public static void Setup()
    {
        foreach (var log in _tempLogs) ConsoleGateway.Log(log);
        _tempLogs = null!;

        _boundLogger = static (scope, message, level) => ConsoleGateway.Log(new StringLogEvent(scope, message, level));

        SetupGfxLogger();
    }

    public static void ToggleGfxLog(bool enabled) => GfxLog.Enabled = enabled;

    private static void SetupGfxLogger()
    {
        if (GfxLog.IsBound) throw new InvalidOperationException("GfxLogger is already active");

        GfxLog.Setup(static (in log) => ConsoleGateway.LogStruct(in log));
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
        GfxLog.Enabled = true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _boundLogger(scope, message, level);


    private static void TempLog(LogScope scope, string message, LogLevel level = LogLevel.Info)
    {
        if (_tempLogs is null) throw new InvalidOperationException(nameof(_tempLogs));
        Console.WriteLine(message);
/*
        _tempLogs.Add(new StringLogEvent(scope, message, level));
        if (_tempLogs.Count > 32)
            Console.Error.WriteLine("Pre Log Buffer count high");

        if (_tempLogs.Count > 512) throw new InvalidOperationException("Pre Log Buffer count high");*/
    }
}