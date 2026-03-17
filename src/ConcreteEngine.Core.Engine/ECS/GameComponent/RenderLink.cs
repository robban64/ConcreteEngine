namespace ConcreteEngine.Core.Engine.ECS.GameComponent;

public struct RenderLink(RenderEntityId renderEntityId) : IGameComponent<RenderLink>
{
    public RenderEntityId RenderEntityId = renderEntityId;
}