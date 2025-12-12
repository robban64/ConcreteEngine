#region

using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities.Components;

[StructLayout(LayoutKind.Sequential)]
public struct AnimationComponent(ModelId model, AnimationId animation)
{
    public ModelId Model = model;
    public AnimationId Animation = animation;
    public int Clip = 0;
    public float Time = 0f;
    public float Speed = 1f;
    public float Duration = 1f;

    public float AdvanceTime(float deltaTime)
    {
        Time += deltaTime * Speed;
        if (Time > Duration)
            Time = 0;

        return Time;
    }
}