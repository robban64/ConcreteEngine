namespace ConcreteEngine.Core.Diagnostics;

public delegate void LoggerDel(LogScope scope, string message, LogLevel level);
public delegate void LogEventDel(in LogEvent logEvent);
