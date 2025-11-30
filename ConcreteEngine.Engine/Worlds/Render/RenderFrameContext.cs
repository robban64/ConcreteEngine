#region

using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal ref struct RenderFrameContext
{
    public Span<DrawEntity> EntitySpan;
    public Span<DrawEntityData> EntityDataSpan;

    public Span<int> EntityByIdSpan;

    public RenderFrameContext(Span<DrawEntity> entitySpan, Span<DrawEntityData> entityDataSpan,
        Span<int> entityByIdSpan)
    {
        EntitySpan = entitySpan;
        EntityDataSpan = entityDataSpan;
        EntityByIdSpan = entityByIdSpan;
    }

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref EntitySpan[EntityByIdSpan[entityId]];

    public DrawEntityWriter GetWriter(int id) => new(ref EntitySpan[id], ref EntityDataSpan[id]);
}

internal ref struct DrawEntityWriter(ref DrawEntity drawEntity, ref DrawEntityData drawEntityData)
{
    public ref DrawEntity DrawEntity = ref drawEntity;
    public ref DrawEntityData DrawEntityData = ref drawEntityData;
}