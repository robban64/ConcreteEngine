using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Time;

public struct FrameStepper(int interval)
{
    private int _ticks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Tick()
    {
        if (_ticks++ < interval) return false;
        _ticks = 0;
        return true;
    }
}