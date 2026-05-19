using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct RenderAnimationComponent(Id16<ModelAnimation> animationId, ushort animationIndex)
    : IRenderComponent<RenderAnimationComponent>, IEquatable<RenderAnimationComponent>
{
    public float Time;
    public short Clip;

    public Id16<ModelAnimation> AnimationId = animationId;
    public ushort AnimationIndex = animationIndex;


    public bool Equals(RenderAnimationComponent other) =>
        AnimationId == other.AnimationId && AnimationIndex == other.AnimationIndex;

    public override bool Equals(object? obj) => obj is RenderAnimationComponent other && Equals(other);
    
    public override readonly int GetHashCode() => HashCode.Combine(AnimationId.Value, AnimationIndex);
}