namespace ConcreteEngine.Core.Time;

internal sealed class DebounceTicker(int ticks)
{
    private int _ticksLeft = ticks;

    public bool Tick()
    {
        if (_ticksLeft > 0) _ticksLeft--;
        return _ticksLeft == 0;
    }
    
    public void Debounce(int ticks) =>  _ticksLeft = int.Max(ticks, _ticksLeft);
}