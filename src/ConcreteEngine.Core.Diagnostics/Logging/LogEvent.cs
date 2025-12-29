using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Diagnostics.Logging;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LogEvent(
    uint id,
    int param0,
    int param1 = 0,
    float fParam0 = 0,
    ushort gen = 0,
    ushort flags = 0,
    LogTopic topic = LogTopic.Unknown,
    LogScope scope = LogScope.Unknown,
    LogAction action = LogAction.Unknown,
    LogLevel level = LogLevel.Trace)
{
    public DateTime Timestamp { get; } = DateTime.Now;
    
    public readonly uint Id = id;
    public readonly int Param0 = param0;
    public readonly int Param1 = param1;
    public readonly float FParam0 = fParam0;
    public readonly ushort Gen = gen;
    public readonly ushort Flags = flags;
    public readonly LogTopic Topic = topic;
    public readonly LogScope Scope = scope;
    public readonly LogAction Action = action;
    public readonly LogLevel Level = level;

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