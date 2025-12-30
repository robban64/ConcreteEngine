using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct FrameStepper(int intervalTicks)
{
    private int _ticks;
    private int _intervalTicks = intervalTicks;

    public int Ticks => _ticks;
    
    public void SetIntervalTicks(int intervalTicks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intervalTicks);
        _intervalTicks = intervalTicks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Tick()
    {
        if (_ticks++ < _intervalTicks) return false;
        _ticks = 0;
        return true;
    }
}