using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameTickTimer(float tickRate)
{
    public long TickId = 0;
    public float Accumulator = 0f;
    public float TickDt = tickRate;

    public float Alpha => TickDt > 0f ? Accumulator / TickDt : 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => Accumulator += dt;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DequeueTick()
    {
        if (Accumulator < TickDt) return false;

        Accumulator -= TickDt;
        TickId++;
        return true;
    }
}