#region

using ConcreteEngine.Common.Diagnostics;

#endregion

namespace Core.DebugTools.Data;

public sealed record LogRecord(
    DateTimeOffset Time,
    LogLevel Level,
    string Message,
    string? Source = null,
    string? Additional = null
);