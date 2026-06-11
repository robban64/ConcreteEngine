using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.GameComponent;


public struct AnimationComponent : IGameComponent<AnimationComponent>
{
    //public short Clip;
    //public Id16<ModelAnimation> RigId;
    
    public float InterpolatedTime;

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Interpolate(float alpha)
    {
        if (Time < PrevTime)
            InterpolatedTime = float.Lerp(PrevTime, Time + Duration, alpha) % Duration;
        else
            InterpolatedTime = float.Lerp(PrevTime, Time, alpha);
    }

}