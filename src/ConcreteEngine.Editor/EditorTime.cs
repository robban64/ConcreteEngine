using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;

namespace ConcreteEngine.Editor;

public static class EditorTime
{
    private const float RateIdle = 1f / 40f; //40Hz
    private const float RateActive = 1f / 60f; //60Hz
    private const float ActivityCooldown = 2.0f;

    //public static float DeltaTime;
    //public static float Fps => DeltaTime / (DeltaTime * DeltaTime + FloatMath.SingularEpsilon);

    private static float _activityTimer;
    private static FrameAccumulator _accumulator = new(RateIdle);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Advance(float frameDelta, out float editorDelta)
    {
        if (_activityTimer > 0f) _activityTimer -= frameDelta;
        if (_activityTimer <= 0f) _accumulator.TickDt = RateIdle;

        _accumulator.Accumulate(frameDelta);
        return _accumulator.DequeueTick(out editorDelta);

    }

    public static void WakeUp()
    {
        _activityTimer = ActivityCooldown;
        _accumulator.TickDt = RateActive;
    }
}