namespace ConcreteEngine.Common.Diagnostics;

public readonly record struct LogEvent(
    uint Id,
    int Param0,
    int Param1 = 0,
    float FParam0 = 0,
    ushort Gen = 0,
    ushort Flags = 0,
    LogTopic Topic = LogTopic.Unknown,
    LogScope Scope = LogScope.Unknown,
    LogAction Action = LogAction.Unknown,
    LogLevel Level = LogLevel.Info)
{
    public long Time { get; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static implicit operator uint(LogEvent log) => log.Id;
}

public readonly record struct LogFilterWildcard(byte Topic, byte Scope, byte Action, byte Level)
{
    public LogFilterWildcard(LogTopic Topic, LogScope Scope, LogAction Action, LogLevel Level)
        : this((byte)Topic, (byte)Scope, (byte)Action, (byte)Level)
    {
    }
}