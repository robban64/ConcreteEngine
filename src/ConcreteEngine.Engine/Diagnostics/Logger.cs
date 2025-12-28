using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Diagnostics;

public static class Logger
{
    private static readonly StructLogParser StructParser = new();

    private static EngineLogger _engineLogger = null!;
    private static LoggerDel _boundLogger = PreLog;

    private static List<StringLogEvent> _preLogs = new(32);

    public static void Setup()
    {
        if (ConsoleGateway.Context == null!) throw new InvalidOperationException();
        _engineLogger = new EngineLogger(ConsoleGateway.Context);

        if (_preLogs.Count > 0)
            _engineLogger.LogMany(CollectionsMarshal.AsSpan(_preLogs));

        _preLogs = null!;
        _boundLogger = DefaultLog;
    }

    public static void SetupGfxLogger()
    {
        if (GfxLog.IsActive) throw new InvalidOperationException("GfxLogger is already active");

        GfxLog.Setup(ForwardGfxLogEvent);
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ForwardGfxLogEvent(in LogEvent log)
    {
        ConsoleGateway.Context.AddLog(StructParser.ToStringLog(in log));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _boundLogger(scope, message, level);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DefaultLog(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _engineLogger.LogString(scope, message, level);


    private static void PreLog(LogScope scope, string message, LogLevel level = LogLevel.Info)
    {
        if (_engineLogger != null! && _engineLogger.IsAttached)
            throw new InvalidOperationException("Logger is attached, use DefaultLog");

        if (_preLogs is null) throw new InvalidOperationException(nameof(_preLogs));

        _preLogs.Add(new StringLogEvent(scope, message, level));
        if (_preLogs.Count > 32)
            Console.Error.WriteLine("Pre Log Buffer count high");

        if (_preLogs.Count > 512) throw new InvalidOperationException("Pre Log Buffer count high");
    }


    public static void ToggleEngineLogger(bool enabled) => _engineLogger.Enabled = enabled;
    public static void ToggleGfxLogger(bool enabled) => GfxLog.Enabled = enabled;
}