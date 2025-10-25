using ConcreteEngine.Common.Diagnostics;

namespace Core.DebugTools.Data;


public sealed record LogRecord(
    DateTimeOffset Time,
    LogLevel Level,
    string Message,
    string? Source = null,
    string? Additional = null
);