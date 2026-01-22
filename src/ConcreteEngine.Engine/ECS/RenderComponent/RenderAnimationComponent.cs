using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct RenderAnimationComponent(AnimationId animation) : IRenderComponent<RenderAnimationComponent>
{
    public float Time;
    public AnimationId Animation = animation;
    public short Clip;
}