using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Core.Engine;

public static class Logger
{
    private static List<StringLogEvent>? _tempLogs = new();
    private static Action<StringLogEvent> _boundLogger = ConsoleLogger;

    internal static void BindLogger(Action<StringLogEvent> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _boundLogger = logger;

        if (_tempLogs is null) return;

        foreach (var log in _tempLogs) logger(log);
        _tempLogs.Clear();
        _tempLogs = null;
    }

    internal static unsafe void BindGfxLogger(delegate*<in LogEvent, void> logger)
    {
        if (GfxLog.IsBound) throw new InvalidOperationException("GfxLogger is already active");
        GfxLog.Setup(logger);
        GfxLog.Enabled = true;

        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Gfx);
/*
        GfxLog.ToggleLog(false, LogTopic.VertexBuffer, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.IndexBuffer, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.Mesh, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.FrameBuffer, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Backend);

        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
        GfxLog.ToggleLog(false, LogTopic.VertexBuffer, LogScope.Gfx);
        GfxLog.ToggleLog(false, LogTopic.IndexBuffer, LogScope.Gfx);
*/
    }

    public static void ToggleGfxLog(bool enabled) => GfxLog.Enabled = enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _boundLogger(new StringLogEvent(scope, message, level));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(StringLogEvent log) => _boundLogger(log);


    private static void ConsoleLogger(StringLogEvent log)
    {
        _tempLogs?.Add(log);
        Console.WriteLine(log.Message);
    }
}