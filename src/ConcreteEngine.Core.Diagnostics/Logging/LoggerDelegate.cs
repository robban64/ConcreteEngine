namespace ConcreteEngine.Core.Diagnostics.Logging;

public delegate void LoggerDel(LogScope scope, string message, LogLevel level);

public delegate void LogEventDel(in LogEvent logEvent);