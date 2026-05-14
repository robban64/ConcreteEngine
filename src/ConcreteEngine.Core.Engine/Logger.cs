using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Core.Engine;

public static class Logger
{
    private static Action<StringLogEvent> _boundLogger = ConsoleLogger;

    internal static void BindLogger(Action<StringLogEvent> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _boundLogger = logger;
    }

    internal static unsafe void BindGfxLogger(delegate*<in LogEvent, void> logger)
    {
        if (GfxLog.IsBound) throw new InvalidOperationException("GfxLogger is already active");
        GfxLog.Setup(logger);
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
        GfxLog.ToggleLog(false, LogTopic.VertexBuffer, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.IndexBuffer, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.VertexBuffer, LogScope.Gfx);
        GfxLog.ToggleLog(false, LogTopic.IndexBuffer, LogScope.Gfx);
        GfxLog.Enabled = true;
    }
    
    public static void ToggleGfxLog(bool enabled) => GfxLog.Enabled = enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogString(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        _boundLogger(new StringLogEvent(scope, message, level));


    private static void ConsoleLogger(StringLogEvent log) => Console.WriteLine(log.Message);
}