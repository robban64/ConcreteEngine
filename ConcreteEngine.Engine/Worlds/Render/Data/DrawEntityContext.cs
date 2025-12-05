#region

using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal readonly ref struct DrawEntityContext(
    int count,
    DrawEntity[] drawEntities,
    DrawEntityData[] drawData,
    int[] byEntityId)
{
    public Span<DrawEntity> EntitySpan => drawEntities.AsSpan(0, count);
    public Span<DrawEntityData> EntityDataSpan => drawData.AsSpan(0, count);
    public Span<int> ByEntityIdSpan => byEntityId.AsSpan(0, count);

    public int Count => EntitySpan.Length;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref drawEntities[byEntityId[entityId]];
    public ref DrawEntityData GetDataByEntityId(EntityId entityId) => ref drawData[byEntityId[entityId]];

    public DrawEntityWriter GetEntityView(int id) => new(ref EntitySpan[id], ref EntityDataSpan[id]);
}

internal ref struct DrawEntityWriter(ref DrawEntity drawEntity, ref DrawEntityData drawEntityData)
{
    public ref DrawEntity DrawEntity = ref drawEntity;
    public ref DrawEntityData DrawEntityData = ref drawEntityData;
}