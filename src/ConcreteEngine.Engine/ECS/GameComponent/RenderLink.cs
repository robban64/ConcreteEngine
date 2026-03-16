using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Engine.ECS.GameComponent;

public struct RenderLink(RenderEntityId renderEntityId) : IGameComponent<RenderLink>
{
    public RenderEntityId RenderEntityId = renderEntityId;
}