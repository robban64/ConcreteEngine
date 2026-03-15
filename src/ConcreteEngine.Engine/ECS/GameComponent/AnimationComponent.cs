using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.ECS.GameComponent;

public struct AnimationComponent : IGameComponent<AnimationComponent>
{
    public float Time;
    public float PrevTime;
    public float Duration;
    public float Speed;
    //public AnimationState State;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceTime(float deltaTime)
    {
        PrevTime = Time;
        Time += deltaTime * Speed;
        if (Time > Duration) Time = 0;
    }
}