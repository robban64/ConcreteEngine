using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Worlds.Render;

internal ref struct RenderFrameContext
{
    public Span<DrawEntity> EntitySpan;
    public Span<int> EntityByIdSpan;
    public ref int WriteCount;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref EntitySpan[EntityByIdSpan[entityId]];
}