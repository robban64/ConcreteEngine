namespace ConcreteEngine.Core.Diagnostics.Logging;

public readonly unsafe ref struct LogBinding(
    Action<StringLogEvent> logger,
    Action<ReadOnlySpan<char>> message,
    delegate*<in LogEvent, void> valueLogger)
{
    public readonly Action<StringLogEvent> Logger = logger;
    public readonly Action<ReadOnlySpan<char>> Message = message;

    public readonly delegate*<in LogEvent, void> ValueLogger = valueLogger;
}
