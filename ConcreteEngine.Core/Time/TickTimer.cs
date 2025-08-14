using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Time;

internal struct TickTimer(float tickDt)
{
    public readonly float TickDt = tickDt;
    public int TickIndex = 0;
    public float Accumulator = 0f;

    // Fraction (0..1) toward next tick for interpolation.
    public float Alpha
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TickDt > 0f ? Accumulator / TickDt : 0f;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => Accumulator += dt;

    public bool TryDequeueTick(out int tickIndex)
    {
        if (Accumulator < TickDt) { tickIndex = -1; return false; }
        Accumulator -= TickDt;
        tickIndex = TickIndex++;
        return true;
    }
    
    //Alternative method
    /*
    public int Drain(int max, Span<int> outTicks)
    {
        int n = 0;
        while (n < max && Accumulator >= TickDt)
        {
            Accumulator -= TickDt;
            outTicks[n++] = TickIndex++;
        }
        return n;
    }
    
    Usage:
        Span<int> buf = stackalloc int[max];
        int count = timer.Drain(max, buf);
        for (int i = 0; i < count; i++)
           ProcessTick(buf[i]);
    */
}