#region

#endregion

namespace ConcreteEngine.Core.Engine.Time;

public struct SimpleTicker(int fps, int ticksPerSec)
{
    public int TickIndex = 0;
    public int Accumulator = 0;

    public void AccumulateGameTime() => Accumulator += fps;

    public bool TryDequeueTick(out int tickIndex)
    {
        if (Accumulator < ticksPerSec)
        {
            tickIndex = -1;
            return false;
        }

        Accumulator -= ticksPerSec;
        tickIndex = TickIndex++;
        return true;
    }
}