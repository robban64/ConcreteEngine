using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Utils;

internal struct TickTimer(float tickDt)
{
    public readonly float TickDt = tickDt;
    public int TickIndex = 0;
    public float Accumulator = 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => Accumulator += dt;

    // If a tick is due, advances and returns its index.
    public bool TryDequeueTick(out int tickIndex)
    {
        if (Accumulator < TickDt) { tickIndex = -1; return false; }
        Accumulator -= TickDt;
        tickIndex = TickIndex++;
        return true;
    }
}