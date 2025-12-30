namespace ConcreteEngine.Core.Diagnostics.Time;

public struct DebounceTicker(int ticksLeft)
{
    public int TicksLeft = ticksLeft;

    public bool Tick()
    {
        if (TicksLeft > 0) TicksLeft--;
        return TicksLeft == 0;
    }

    public void Debounce(int ticks) => TicksLeft = int.Max(ticks, TicksLeft);
}