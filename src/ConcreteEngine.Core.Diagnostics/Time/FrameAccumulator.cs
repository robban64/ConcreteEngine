using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameAccumulator(double tickRate)
{
    public double Accumulator = 0.0;
    public double TickDt = tickRate;

    public readonly double Alpha
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TickDt > 0.0 ? Accumulator / TickDt : 0.0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(double dt) => Accumulator += dt;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDrainTick()
    {
        if (Accumulator < TickDt) return false;
        Accumulator = 0;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DequeueTick(out double dt)
    {
        if (Accumulator < TickDt)
        {
            dt = 0;
            return false;
        }

        Accumulator -= TickDt;
        dt = TickDt;
        return true;
    }
}