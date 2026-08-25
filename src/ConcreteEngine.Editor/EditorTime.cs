using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Editor;

public static class EditorTime
{
    private const double RateIdle = 1.0 / 40.0; //40Hz
    private const double RateActive = 1.0 / 60.0; //60Hz
    private const double ActivityCooldown = 2.0;
    
    private static double _activityTimer;
    private static FrameAccumulator _accumulator = new(RateIdle);

    public static float Delta { get; private set; }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Advance(double frameDelta, out float editorDelta)
    {
        if (_activityTimer > 0.0) _activityTimer -= frameDelta;
        if (_activityTimer <= 0.0) _accumulator.TickDt = RateIdle;

        _accumulator.Accumulate(frameDelta);
        var shouldAdvance = _accumulator.DequeueTick(out var dt);
        editorDelta = (float)dt;
        Delta = editorDelta;
        return shouldAdvance;
    }

    public static void WakeUp()
    {
        _activityTimer = ActivityCooldown;
        _accumulator.TickDt = RateActive;
    }
}