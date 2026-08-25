using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameStepper(int intervalTicks)
{
    public int Ticks = 0;
    public int IntervalTicks = intervalTicks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIntervalTicks(int intervalTicks, int ticks = 0)
    {
        IntervalTicks = intervalTicks;
        Ticks = ticks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Tick()
    {
        if (IntervalTicks == 0 || Ticks++ < IntervalTicks) return false;
        Ticks = 0;
        return true;
    }
}