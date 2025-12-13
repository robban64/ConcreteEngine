#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal ref struct DrawEntityEnumerator(DrawEntityContext ctx)
{
    private readonly DrawEntityContext _ctx = ctx;
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _ctx.VisibleCount;
    
    public DrawEntityView Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var idx = _ctx.VisibleIndexSpan[_i];
            return new DrawEntityView(idx, ref _ctx.EntitySpan[idx]);
        }
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
    int totalCount,
    int visibleCount,
    DrawEntity[] drawEntities,
    int[] byEntityId,
    int[] visibleIndices)
{
    public readonly Span<DrawEntity> EntitySpan = drawEntities.AsSpan(0, totalCount);
    public readonly Span<int> ByEntityIdSpan = byEntityId.AsSpan(0, totalCount);
    public readonly Span<int> VisibleIndexSpan = visibleIndices.AsSpan(0, visibleCount);

    public readonly int TotalCount = totalCount;
    public readonly int VisibleCount = visibleCount;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref EntitySpan[ByEntityIdSpan[entityId]];
    
    public DrawEntityEnumerator GetEnumerator() => new(this);
}