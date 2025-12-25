namespace ConcreteEngine.Shared.Diagnostics;

public sealed record StringLogEvent(LogScope Scope, string Message, LogLevel Level = LogLevel.Info)
{
    public DateTime Timestamp { get; } = DateTime.Now;

    public bool IsPlain() => Level == LogLevel.None;
    
    public static StringLogEvent MakePlain(string message) => new(LogScope.Unknown, message, LogLevel.None);
}