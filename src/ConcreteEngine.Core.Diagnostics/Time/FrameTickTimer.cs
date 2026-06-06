using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameTickTimer(float tickRate)
{
    public float Accumulator = 0f;
    public readonly float TickDt = tickRate;

    public readonly float Alpha
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TickDt > 0f ? Accumulator / TickDt : 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => Accumulator += dt;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DequeueTick(out  float dt)
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