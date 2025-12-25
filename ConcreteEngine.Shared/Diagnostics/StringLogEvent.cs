namespace ConcreteEngine.Shared.Diagnostics;

public sealed record StringLogEvent(LogScope Scope, string Message, LogLevel Level = LogLevel.Info)
{
    public long Timestamp { get; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}