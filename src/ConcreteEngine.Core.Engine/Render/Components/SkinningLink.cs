using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Graphics.Animations;

namespace ConcreteEngine.Core.Engine.Render.Components;

public struct SkinningLink(Id16<AnimationInstance> animationId) : IRenderComponent<SkinningLink>
{
    public readonly Id16<AnimationInstance> AnimationId = animationId;
    public ushort AnimationSlot;
}