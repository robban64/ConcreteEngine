using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.GameComponent;


public struct AnimationComponent(Id16<ModelRig> rigId, short clip = 0)
    : IGameComponent<AnimationComponent>
{
    public Id16<ModelRig> RigId = rigId;
    //public ushort RigInstanceId = rigInstanceId;
    public short Clip = clip;
    
    public float Speed;
    public float Duration;

    public float Time;
    public float PrevTime;
    public float InterpolatedTime;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AdvanceTime(float deltaTime)
    {
        PrevTime = Time;
        Time += deltaTime * Speed;
        if (Time > Duration) Time = 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Interpolate(float alpha)
    {
        if (Time < PrevTime)
            InterpolatedTime = float.Lerp(PrevTime, Time + Duration, alpha) % Duration;
        else
            InterpolatedTime = float.Lerp(PrevTime, Time, alpha);
    }

}