using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Time;

public struct FrameTickTimer(float tickDt)
{
    public int TickIndex = 0;
    public float Accumulator = 0f;

    public float Alpha => tickDt > 0f ? Accumulator / tickDt : 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => Accumulator += dt;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DequeueTick()
    {
        if (Accumulator < tickDt) return false;

        Accumulator -= tickDt;
        TickIndex++;
        return true;
    }
}