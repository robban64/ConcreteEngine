namespace Core.DebugTools.Data;

public enum LogLevel
{
    Verbose = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public sealed record LogRecord(
    DateTimeOffset Time,
    LogLevel Level,
    string Message,
    string? Source = null,
    string? Additional = null
);