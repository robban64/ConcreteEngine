#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Core.Time;

public struct GameTickTimer(int fps, int ticksPerSec)
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