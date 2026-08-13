using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Engine.Graphics.Animations;

[StructLayout(LayoutKind.Sequential)]
public struct AnimationTime
{
    public float Time;
    public float PrevTime;

    public float Duration;
    public float TicksPerSecond;

    public void SetClip(float duration, float ticksPerSecond)
    {
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
        Time = 0;
        PrevTime = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTime(float deltaTime)
    {
        PrevTime = Time;
        Time += deltaTime * TicksPerSecond;
        if (Time > Duration) Time = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float Interpolate(float alpha)
    {
        if (Time < PrevTime)
            return float.Lerp(PrevTime, Time + Duration, alpha) % Duration;

        return float.Lerp(PrevTime, Time, alpha);
    }
}