using System.Runtime.InteropServices;
using ConcreteEngine.Engine.Worlds.Data;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct RenderAnimationComponent : IRenderComponent<RenderAnimationComponent>
{
    public float Time;
    public float Duration;
    public float Speed;
    public AnimationId Animation;
    public short Clip;

    public RenderAnimationComponent(AnimationId animation, float speed, float duration)
    {
        Animation = animation;
        Clip = 0;
        Speed = speed;
        Duration = duration;
        Time = 0f;
    }
}