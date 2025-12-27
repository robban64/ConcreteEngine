using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Diagnostics;

[StructLayout(LayoutKind.Sequential)]
public readonly struct LogEvent
{
    public readonly uint Id;
    public readonly int Param0;
    public readonly int Param1;
    public readonly float FParam0;
    public readonly ushort Gen;
    public readonly ushort Flags;
    public readonly LogTopic Topic;
    public readonly LogScope Scope;
    public readonly LogAction Action;
    public readonly LogLevel Level;

    public LogEvent(uint id,
        int param0,
        int param1 = 0,
        float fParam0 = 0,
        ushort gen = 0,
        ushort flags = 0,
        LogTopic topic = LogTopic.Unknown,
        LogScope scope = LogScope.Unknown,
        LogAction action = LogAction.Unknown,
        LogLevel level = LogLevel.Info)
    {
        Id = id;
        Param0 = param0;
        Param1 = param1;
        FParam0 = fParam0;
        Gen = gen;
        Flags = flags;
        Topic = topic;
        Scope = scope;
        Action = action;
        Level = level;
    }
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