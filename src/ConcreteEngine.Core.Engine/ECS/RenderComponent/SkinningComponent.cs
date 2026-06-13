using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct SkinningComponent(Id16<AnimationInstance> animationId) : IRenderComponent<SkinningComponent>
{
    public readonly Id16<AnimationInstance> AnimationId = animationId;
}