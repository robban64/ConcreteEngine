using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics.Animations;

namespace ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

public struct SkinningLink(Id16<AnimationInstance> animationId) : IRenderComponent<SkinningLink>
{
    public readonly Id16<AnimationInstance> AnimationId = animationId;
    public ushort AnimationSlot;
}