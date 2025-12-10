#region

#endregion

#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Engine.Time.Tickers;

internal struct FrameTickTimer(float tickDt)
{
    private int _tickIndex = 0;
    private float _accumulator = 0f;

    //public float Alpha => tickDt > 0f ? _accumulator / tickDt : 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Accumulate(float dt) => _accumulator += dt;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /*
    public int DrainAllTicks()
    {
        int n = 0;
        while (TryDequeueTick(out _)) n++;
        return n;
    }
    */
}