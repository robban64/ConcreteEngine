using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameStepper(int intervalTicks)
{
    private short _ticks;
    public short IntervalTicks { get; private set; } = (short)intervalTicks;


    public void SetIntervalTicks(int intervalTicks, int ticks = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(intervalTicks);
        IntervalTicks = (short)intervalTicks;
        _ticks = (short)ticks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Tick()
    {
        if (IntervalTicks == 0 || _ticks++ < IntervalTicks) return false;
        _ticks = 0;
        return true;
    }
}