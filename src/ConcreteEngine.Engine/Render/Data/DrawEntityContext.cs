using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Engine.Render.Data;

internal ref struct DrawEntityEnumerator(DrawEntityContext ctx)
{
    private readonly DrawEntityContext _ctx = ctx;
    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _ctx.DrawEntities.Length;

    public readonly ref DrawEntity Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _ctx.DrawEntities[_i];
    }

    public DrawEntityEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }
}

internal readonly ref struct DrawEntityContext
{
    public readonly Span<DrawEntity> DrawEntities;
    public readonly Span<RenderEntityId> VisibleEntities;
    public readonly Span<int> VisibleByIndices;

    public DrawEntityContext(
        int visibleLength,
        DrawEntity[] drawEntities,
        RenderEntityId[] visibleEntities,
        int[] visibleByIndices)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(visibleLength, visibleEntities.Length);
        if (drawEntities.Length != visibleEntities.Length || visibleByIndices.Length != visibleEntities.Length)
            throw new ArgumentOutOfRangeException();

        DrawEntities = drawEntities.AsSpan(0, visibleLength);
        VisibleEntities = visibleEntities.AsSpan(0, visibleLength);
        VisibleByIndices = visibleByIndices.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<DrawEntity> TryGetVisible(RenderEntityId entity)
    {
        var index = VisibleByIndices[entity.Index()];
        if (index < 0) return ValuePtr<DrawEntity>.Null;
        return new ValuePtr<DrawEntity>(ref DrawEntities[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityEnumerator GetEnumerator() => new(this);
}