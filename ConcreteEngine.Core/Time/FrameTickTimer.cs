using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Time;

internal struct FrameTickTimer(float tickDt)
{
    private int _tickIndex = 0;
    private float _accumulator = 0f;

    // Fraction (0..1) toward next tick for interpolation.
    public float Alpha
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => tickDt > 0f ? _accumulator / tickDt : 0f;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => _accumulator += dt;

    public bool TryDequeueTick(out int tickIndex)
    {
        if (_accumulator < tickDt) { tickIndex = -1; return false; }
        _accumulator -= tickDt;
        tickIndex = _tickIndex++;
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