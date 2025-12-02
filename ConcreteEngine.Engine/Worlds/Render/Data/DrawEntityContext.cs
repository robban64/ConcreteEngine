#region

using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal ref struct DrawEntityContext(int count)
{
    public readonly Span<DrawEntity> EntitySpan => DrawEntityStore.Entities.AsSpan(0, count);
    public readonly Span<DrawEntityData> EntityDataSpan => DrawEntityStore.EntityData.AsSpan(0, count);
    public readonly Span<int> EntityByIdSpan => DrawEntityStore.ByEntityId.AsSpan(0, count);

    public readonly int Count => EntitySpan.Length;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref EntitySpan[EntityByIdSpan[entityId]];
    public readonly DrawEntityWriter GetWriter(int id) => new(ref EntitySpan[id], ref EntityDataSpan[id]);
}

internal ref struct DrawEntityWriter(ref DrawEntity drawEntity, ref DrawEntityData drawEntityData)
{
    public ref DrawEntity DrawEntity = ref drawEntity;
    public ref DrawEntityData DrawEntityData = ref drawEntityData;
}
