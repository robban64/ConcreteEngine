#region

#endregion

namespace ConcreteEngine.Core.Time.Tickers;

internal sealed class FrameTickTimer(float tickDt)
{
    private int _tickIndex = 0;
    private float _accumulator = 0f;

    public float Alpha => tickDt > 0f ? _accumulator / tickDt : 0f;

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
}