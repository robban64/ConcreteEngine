#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Core.Time;

internal sealed class FrameTickTimer(float tickDt)
{
    private int _tickIndex = 0;
    private float _accumulator = 0f;

    public float Alpha
    {
        get => tickDt > 0f ? _accumulator / tickDt : 0f;
    }


    public void Accumulate(float dt) => _accumulator += dt;

    public int DrainAllTicks()
    {
        int n = 0;
        while (TryDequeueTick(out _)) n++;
        return n;
    }

    public bool TryDequeueTick(out int tickIndex)
    {
        if (_accumulator < tickDt)
        {
            tickIndex = -1;
            return false;
        }

        _accumulator -= tickDt;
        tickIndex = _tickIndex++;
        return true;
    }


    //Alternative method
/*
     [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private int DrainTick(int max, Span<int> outTicks)
   {
       Span<int> buf = stackalloc int[max];
       int count = Drain(max, buf);
       for (int i = 0; i < count; i++) ; //TODO
   }


   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private int Drain(int max, Span<int> outTicks)
   {
       int n = 0;
       while (n < max && _accumulator >= tickDt)
       {
           _accumulator -= tickDt;
           outTicks[n++] = _tickIndex++;
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