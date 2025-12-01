#region

using System.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal ref struct DrawEntityContext(
    int count,
    DrawEntity[] entities,
    DrawEntityData[] entityData,
    int[] entityById)
{
    private readonly DrawEntity[] _entities = entities;
    private readonly DrawEntityData[] _entityData = entityData;
    private readonly int[] _entityById = entityById;

    public Span<DrawEntity> EntitySpan => _entities.AsSpan(0, count);
    public Span<DrawEntityData> EntityDataSpan => _entityData.AsSpan(0, count);
    public Span<int> EntityByIdSpan => _entityById.AsSpan(0, count);

    public int Count => EntitySpan.Length;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref EntitySpan[EntityByIdSpan[entityId]];
    public DrawEntityWriter GetWriter(int id) => new(ref EntitySpan[id], ref EntityDataSpan[id]);
}

internal ref struct DrawEntityWriter(ref DrawEntity drawEntity, ref DrawEntityData drawEntityData)
{
    public ref DrawEntity DrawEntity = ref drawEntity;
    public ref DrawEntityData DrawEntityData = ref drawEntityData;
}