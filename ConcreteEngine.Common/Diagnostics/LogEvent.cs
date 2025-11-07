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
/*
public readonly record struct LogFilterWildcard
{
    public readonly uint Mask;
    public readonly uint Value;

    public LogFilterWildcard(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
    {
        Value = Pack((byte)topic, (byte)scope, (byte)action, (byte)level);
        uint mask = 0;
        if ((byte)topic   != 0) mask |= 0x000000FFu;
        if ((byte)scope  != 0) mask |= 0x0000FF00u;
        if ((byte)action != 0) mask |= 0x00FF0000u;
        if ((byte)level != 0) mask |= 0xFF000000u;
        Mask = mask;

    }

    public static bool ShouldIgnore(in LogEvent log, LogFilterWildcard[] filter)
    {
        var packed = Pack((byte)log.Topic, (byte)log.Scope, (byte)log.Action, (byte)log.Level);
        foreach (var r in filter)
        {
            if (((packed ^ r.Value) & r.Mask) == 0) return true;
        }
        return false;
    }

    static uint Pack(byte topic, byte scope, byte action, byte level) =>
        (uint)topic | ((uint)scope << 8) | ((uint)action << 16) | ((uint)level << 24);

}*/