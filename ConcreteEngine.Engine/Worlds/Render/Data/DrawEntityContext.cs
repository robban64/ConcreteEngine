using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS;

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
    Span<RenderEntityId> entityIndices,
    Span<int> byEntityId)
{
    public readonly Span<DrawEntity> EntitySpan = drawEntities;
    public readonly Span<RenderEntityId> EntityIndices = entityIndices;
    public readonly Span<int> ByEntityIdSpan = byEntityId;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId renderEntityId) => ByEntityIdSpan[renderEntityId] != -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawEntity GetByEntityId(RenderEntityId renderEntityId) => ref EntitySpan[ByEntityIdSpan[renderEntityId]];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityEnumerator GetEnumerator() => new(this);
}