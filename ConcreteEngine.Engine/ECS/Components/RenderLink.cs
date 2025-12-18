using ConcreteEngine.Engine.ECS.Game;
using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.ECS.Components;

public struct RenderLink : IGameComponent<RenderLink>
{
    public EntityId EntityId;
}