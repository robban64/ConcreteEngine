using System.Runtime.CompilerServices;
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

public readonly struct LogFilterWildcard
{
    public readonly uint Mask;
    public readonly uint Value;

    public LogFilterWildcard(byte topic, byte scope, byte action, byte level)
    {
        Value = Pack(topic, scope, action, level);
        uint mask = 0;
        if (topic != 0) mask |= BitMath.ByteMaskU32(0);
        if (scope != 0) mask |= BitMath.ByteMaskU32(1);
        if (action != 0) mask |= BitMath.ByteMaskU32(2);
        if (level != 0) mask |= BitMath.ByteMaskU32(3);
        Mask = mask;
    }

    public LogFilterWildcard(LogTopic topic, LogScope scope, LogAction action, LogLevel level)
        : this((byte)topic, (byte)scope, (byte)action, (byte)level)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexAt(uint packed, List<LogFilterWildcard> filter)
    {
        for (var i = 0; i < filter.Count; i++)
        {
            var f = filter[i];
            if (((packed ^ f.Value) & f.Mask) == 0) return i;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack(byte topic, byte scope, byte action, byte level) =>
        (uint)topic | ((uint)scope << 8) | ((uint)action << 16) | ((uint)level << 24);
}