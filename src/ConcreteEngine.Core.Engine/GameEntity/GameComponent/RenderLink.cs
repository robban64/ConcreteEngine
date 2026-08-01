using ConcreteEngine.Core.Engine.RenderEntity;

namespace ConcreteEngine.Core.Engine.GameEntity.GameComponent;

public struct RenderLink(RenderEntityId renderEntityId) : IGameComponent<RenderLink>
{
    public RenderEntityId RenderEntityId = renderEntityId;
}