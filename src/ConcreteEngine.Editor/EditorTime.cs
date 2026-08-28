using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Editor;

public static class EditorTime
{
    private const double RateIdle = 1.0 / 20.0; //20Hz
    private const double RateHover = 1.0 / 40.0; // 40Hz
    private const double RateActive = 1.0 / 60.0; //60Hz
    private const double ActivityCooldown = 4.0;
    
    private static double _activityTimer;
    private static FrameAccumulator _accumulator;
    public static float Delta { get; private set; }

    internal static void Initialize()
    {
        _accumulator = new FrameAccumulator(RateActive);
        _activityTimer = 8.0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Advance(double frameDelta, out float editorDelta)
    {
        if (_activityTimer > 0.0) _activityTimer -= frameDelta;
        if (_activityTimer <= 0.0) _accumulator.TickDt = RateIdle;

        _accumulator.Accumulate(frameDelta);
        var shouldAdvance = _accumulator.DequeueTick(out var dt);
        Delta = editorDelta = (float)dt;
        return shouldAdvance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FullWakeUp()
    {
        _activityTimer = ActivityCooldown;
        _accumulator.TickDt = RateActive;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MediumWakeUp()
    {
        if(_accumulator.TickDt >= RateHover) return;
        _accumulator.TickDt = RateHover;
        _activityTimer = ActivityCooldown;
    }

}