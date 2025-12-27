using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.ECS.GameComponent;

public enum AnimationState
{
    None,
    Pause,
    Play,
    Blending,
}

public struct AnimationComponent : IGameComponent<AnimationComponent>
{
    public float Time;
    public float PrevTime;
    public float Duration;
    public float Speed;
    public short Clip;
    public AnimationState State;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTime(float deltaTime)
    {
        PrevTime = Time;
        Time += deltaTime * Speed;
        if (Time > Duration) Time = 0;
    }
}