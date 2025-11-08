using ConcreteEngine.Common.Numerics.Maths;

namespace ConcreteEngine.Shared.Diagnostics;

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

public readonly struct LogFilterWildcardV2
{
    public readonly uint Mask;
    public readonly uint Value;
    public LogFilterWildcardV2(byte topic, byte scope, byte action, byte level)
    {
        Value = Pack(topic, scope, action, level);
        uint mask = 0;
        if (topic != 0) mask |= BitMath.ByteMaskU32(0);
        if (scope != 0) mask |= BitMath.ByteMaskU32(1);
        if (action != 0)mask |= BitMath.ByteMaskU32(2);
        if (level != 0) mask |= BitMath.ByteMaskU32(3);
        /*
        if ((byte)topic != 0) mask |= 0x000000FFu;
        if ((byte)scope != 0) mask |= 0x0000FF00u;
        if ((byte)action != 0) mask |= 0x00FF0000u;
        if ((byte)level != 0) mask |= 0xFF000000u;
        */
        Mask = mask;
    }

    public LogFilterWildcardV2(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
        : this((byte)topic, (byte)scope, (byte)action, (byte)level)
    {
        
    }


    public static bool ShouldIgnore(in LogEvent log, ReadOnlySpan<LogFilterWildcardV2> filter)
    {
        
        var packed = Pack((byte)log.Topic, (byte)log.Scope, (byte)log.Action, (byte)log.Level);
        foreach (ref readonly var f in filter)
        {
            if (((packed ^ f.Value) & f.Mask) == 0) return true;
        }

        return false;
    }

    private static uint Pack(byte topic, byte scope, byte action, byte level) =>
        (uint)topic | ((uint)scope << 8) | ((uint)action << 16) | ((uint)level << 24);
}