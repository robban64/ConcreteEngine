using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal ref struct DrawEntityEnumerator(DrawEntityContext ctx)
{
    private readonly DrawEntityContext _ctx = ctx;
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _ctx.EntitySpan.Length;

    public readonly DrawEntityView Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_i, ref _ctx.EntitySpan[_i]);
    }

    public DrawEntityEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }
}

internal ref struct DrawEntityView(int idx, ref DrawEntity entity)
{
    public readonly int Index = idx;
    public ref DrawEntity DrawEntity = ref entity;
}

internal ref struct DrawEntityContext(
    WorldEntities worldEntities,
    Span<DrawEntity> drawEntities,
    Span<EntityId> entityIndices,
    Span<int> byEntityId)
{
    public readonly WorldEntities WorldEntities = worldEntities;
    public Span<DrawEntity> EntitySpan = drawEntities;
    public Span<EntityId> EntityIndices = entityIndices;
    public Span<int> ByEntityIdSpan = byEntityId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsVisible(EntityId entityId) => ByEntityIdSpan[entityId] != -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref DrawEntity GetByEntityId(EntityId entityId) => ref EntitySpan[ByEntityIdSpan[entityId]];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DrawEntityEnumerator GetEnumerator() => new(this);
}