using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine;

public abstract class RenderSystem
{
    public abstract Terrain Terrain { get; }

    public abstract int VisibleCount { get; }
    public abstract ReadOnlySpan<RenderEntityId> VisibleEntities();
}