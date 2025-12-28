namespace ConcreteEngine.Core.Diagnostics.Logging;

public sealed record StringLogEvent
{
    public StringLogEvent(LogScope Scope, string Message, LogLevel Level = LogLevel.Info)
    {
        this.Scope = Scope;
        this.Message = Message;
        this.Level = Level;
    }
    
    public StringLogEvent(){}

    public DateTime Timestamp { get; init; } = DateTime.Now;
    public LogScope Scope { get; init; }
    public string Message { get; init; } = string.Empty;
    public LogLevel Level { get; init; }

    public bool IsPlain() => Level == LogLevel.None;

    public static StringLogEvent MakePlain(string message) => new(LogScope.Unknown, message, LogLevel.None);

}