using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Core.Engine;

public sealed class Logger
{
    private static readonly Logger Instance = new();

    private bool _isBound;

    private List<StringLogEvent>? _tempLogs = new();

    private Action<StringLogEvent> _logDel = ConsoleLogger;
    private Action<ReadOnlySpan<char>> _messageDel = Console.WriteLine;

    private Logger() { }

    public static bool IsBound => Instance._isBound;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Message(ReadOnlySpan<char> message) => Instance._messageDel(message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LogScope scope, string message, LogLevel level = LogLevel.Info) =>
        Instance._logDel(new StringLogEvent(scope, message, level));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(StringLogEvent log) => Instance._logDel(log);

    private unsafe void Bind(LogBinding binding)
    {
        if (_isBound) Throwers.InvalidOperation("Logger can only be bound once");
        if (binding.ValueLogger == null) Throwers.InvalidArgument(nameof(binding));

        _logDel = binding.Logger;
        _messageDel = binding.Message;
        BindGfxLogger(binding.ValueLogger);

        if (_tempLogs is not null)
        {
            foreach (var log in _tempLogs) binding.Logger(log);
            _tempLogs.Clear();
            _tempLogs = null;
        }

        _isBound = true;
    }

    internal static void BindLogger(LogBinding binding) => Instance.Bind(binding);

    private static unsafe void BindGfxLogger(delegate*<in LogEvent, void> logger)
    {
        if (GfxLog.IsBound) throw new InvalidOperationException("GfxLogger is already active");
        GfxLog.Setup(logger);
        GfxLog.Enabled = true;

        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Gfx);
    }

    public static void ToggleGfxLog(bool enabled) => GfxLog.Enabled = enabled;


    private static void ConsoleLogger(StringLogEvent log)
    {
        Instance._tempLogs?.Add(log);
        Console.WriteLine(log.Message);
    }
}