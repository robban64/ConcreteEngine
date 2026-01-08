using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Engine.ECS;

namespace ConcreteEngine.Engine.Render.Data;

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
    
    
    internal ref struct DrawEntityView(int idx, ref DrawEntity entity)
    {
        public ref DrawEntity DrawEntity = ref entity;
        public readonly int Index = idx;
    }
}

internal readonly ref struct DrawEntityContext
{
    public int Length => EntitySpan.Length;

    public readonly Span<DrawEntity> EntitySpan;
    public readonly Span<int> ByEntityIdSpan;

    public readonly Span<RenderEntityId> EntityIndices;
    public readonly Span<Matrix4x4> EntityWorld;

    public DrawEntityContext(
        int visibleLength,
        int ecsLength,
        Span<DrawEntity> drawEntities,
        Span<int> byEntityId,
        Span<RenderEntityId> entityIndices,
        Span<Matrix4x4> entityWorld)
    {
        if (drawEntities.Length != byEntityId.Length || drawEntities.Length != entityIndices.Length ||
            drawEntities.Length != entityWorld.Length)
            throw new ArgumentOutOfRangeException();

        EntitySpan = drawEntities.Slice(0, visibleLength);
        EntityIndices = entityIndices.Slice(0, visibleLength);
        ByEntityIdSpan = byEntityId.Slice(0,ecsLength+1);
        EntityWorld = entityWorld.Slice(0,ecsLength+1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<DrawEntity> TryGetVisible(RenderEntityId entity)
    {
        var index = ByEntityIdSpan[entity];
        if (index == -1) return ValuePtr<DrawEntity>.Null;
        return new ValuePtr<DrawEntity>(ref EntitySpan[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref DrawEntity GetByEntityId(RenderEntityId renderEntityId) =>
        ref EntitySpan[ByEntityIdSpan[renderEntityId]];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityEnumerator GetEnumerator() => new(this);
}