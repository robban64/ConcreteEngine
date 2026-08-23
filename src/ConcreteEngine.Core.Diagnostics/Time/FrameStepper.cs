using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameStepper(int intervalTicks)
{
    private int _ticks;
    public int IntervalTicks { get; private set; } = intervalTicks;


    public void SetIntervalTicks(int intervalTicks, int ticks = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(intervalTicks);
        IntervalTicks = intervalTicks;
        _ticks = ticks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Tick()
    {
        if (IntervalTicks == 0 || _ticks++ < IntervalTicks) return false;
        _ticks = 0;
        return true;
    }
}