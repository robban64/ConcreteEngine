using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Core.Engine;

public abstract class RenderSystem
{
    public abstract int VisibleCount { get; }
    public abstract ReadOnlySpan<RenderEntityId> VisibleEntities();
}