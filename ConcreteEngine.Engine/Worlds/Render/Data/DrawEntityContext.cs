#region

using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

using Store = ConcreteEngine.Engine.Worlds.Render.DrawEntityStore;

internal readonly ref struct DrawEntityContext(int count)
{
    public Span<DrawEntity> EntitySpan => Store.Entities.AsSpan(0, count);
    public Span<DrawEntityData> EntityDataSpan => Store.EntityData.AsSpan(0, count);
    public Span<int> EntityByIdSpan => Store.ByEntityId.AsSpan(0, count);

    public int Count => EntitySpan.Length;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref Store.Entities[Store.ByEntityId[entityId]];
    public DrawEntityWriter GetEntityView(int id) => new(ref EntitySpan[id], ref EntityDataSpan[id]);
}

internal ref struct DrawEntityWriter(ref DrawEntity drawEntity, ref DrawEntityData drawEntityData)
{
    public ref DrawEntity DrawEntity = ref drawEntity;
    public ref DrawEntityData DrawEntityData = ref drawEntityData;
}