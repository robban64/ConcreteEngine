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

internal readonly ref struct DrawEntityContext(
    Span<DrawEntity> drawEntities,
    Span<EntityHandle> entityIndices,
    Span<int> byEntityId)
{
    public readonly Span<DrawEntity> EntitySpan = drawEntities;
    public readonly Span<EntityHandle> EntityIndices = entityIndices;
    public readonly Span<int> ByEntityIdSpan = byEntityId;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(EntityHandle entityHandle) => ByEntityIdSpan[entityHandle] != -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawEntity GetByEntityId(EntityHandle entityHandle) => ref EntitySpan[ByEntityIdSpan[entityHandle]];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityEnumerator GetEnumerator() => new(this);
}