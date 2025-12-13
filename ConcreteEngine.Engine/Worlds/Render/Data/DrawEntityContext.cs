#region

using ConcreteEngine.Engine.Worlds.Entities;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal readonly ref struct DrawEntityContext
{
    private readonly DrawEntity[] _drawEntities;
    private readonly int[] _visibleIndices;
    private readonly int[] _byEntityId;
    private readonly int _count;
    private readonly int _indicesCount;

    public DrawEntityContext(int count,
        int indicesCount,
        DrawEntity[] drawEntities,
        int[] byEntityId,
        int[] visibleIndices)
    {
        _count = count;
        _indicesCount = indicesCount;
        _drawEntities = drawEntities;
        _visibleIndices = visibleIndices;
        _byEntityId = byEntityId;
    }

    public Span<DrawEntity> EntitySpan => _drawEntities.AsSpan(0, _count);
    public Span<int> ByEntityIdSpan => _byEntityId.AsSpan(0, _count);
    public Span<int> VisibleIndexSpan => _visibleIndices.AsSpan(0, _indicesCount);

    public int Count => EntitySpan.Length;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref _drawEntities[_byEntityId[entityId]];
}
/*

internal ref struct DrawEntityContextSpan(
    int count,
    int visibleCount,
    Span<DrawEntity> drawEntities,
    Span<int> byEntityId,
    Span<int> visibleIndices)
{
    public Span<DrawEntity> DrawEntities = drawEntities;
    public Span<int> ByEntityId = byEntityId;
    public Span<int> VisibleIndices = visibleIndices;

    public int Count = count;
    public int VisibleCount = visibleCount;

    public ref DrawEntity GetByEntityId(EntityId entityId) => ref DrawEntities[ByEntityId[entityId]];
}*/