using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct AnimationComponent : IEntityComponent
{
    public float Time;
    public float Duration;
    public float Speed;
    public AnimationId Animation;
    public short Clip;

    public AnimationComponent(AnimationId animation, float speed, float duration)
    {
        Animation = animation;
        Clip = 0;
        Speed = speed;
        Duration = duration;
        Time = 0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float AdvanceTime(float deltaTime)
    {
        Time += deltaTime * Speed;
        if (Time > Duration)
            Time = 0;

        return Time;
    }
}