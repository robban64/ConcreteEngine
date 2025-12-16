using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal sealed class EntityCoreStore
{
    //private int _entityIdx = 0;
    private EntityHandle Create() => new(++_idx);
    private int _idx = 0;

    private EntityId[] _entities;
    private RenderSourceComponent[] _sources;
    private Transform[] _transforms;
    private BoxComponent[] _boxes;

    private Stack<int> _free = [];

    public int ActiveCount => int.Max(0, _idx - 1 - _free.Count);
    public int Count => _idx;

    public bool IsDirty { get; private set; }


    internal EntityCoreStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = new EntityId[initialCapacity];
        _sources = new RenderSourceComponent[initialCapacity];
        _transforms = new Transform[initialCapacity];
        _boxes = new BoxComponent[initialCapacity];
    }

    public EntityId GetEntity(EntityHandle e) => _entities[e - 1];

    // Getters
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderSourceComponent GetSourceById(EntityHandle e) => ref _sources[e - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransformById(EntityHandle e) => ref _transforms[e - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoxComponent GetBoxById(EntityHandle e) => ref _boxes[e - 1];

    // Spans
    public Span<EntityId> GetEntitySpan() => _entities.AsSpan(0, _idx);
    public Span<RenderSourceComponent> GetSourceSpan() => _sources.AsSpan(0, _idx);
    public Span<Transform> GetTransformSpan() => _transforms.AsSpan(0, _idx);
    public Span<BoxComponent> GetBoxSpan() => _boxes.AsSpan(0, _idx);

    // Views
    public EntityView GetEntityView(EntityHandle e)
    {
        var idx = e - 1;
        if ((uint)idx >= _entities.Length) throw new IndexOutOfRangeException();
        return new EntityView(e, ref _sources[idx], ref _transforms[idx], ref _boxes[idx]);
    }

    public EntitiesReadView GetReadView() =>
        new(_idx, _sources.AsSpan(0, _idx), _transforms.AsSpan(0, _idx), _boxes.AsSpan(0, _idx));

    public EntitiesCoreView GetCoreView() =>
        new(_idx, _sources.AsSpan(0, _idx), _transforms.AsSpan(0, _idx), _boxes.AsSpan(0, _idx));


    public EntityId AddEntity(EntityKind kind, in CoreComponent component)
    {
        //var idx = _free.Count > 0 ? _free.Pop() : Create();
        var index = _idx;
        var entity = new EntityId(Create(), 0, kind);
        EnsureCapacity(1);

        _entities[index] = entity;
        _sources[index] = component.RenderSource;
        _transforms[index] = component.Transform;
        _boxes[index] = component.Box;

        IsDirty = true;
        return entity;
    }

    public void Remove(EntityHandle e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Value, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Value, _idx, nameof(e));
    }

    internal void EndTick()
    {
        IsDirty = false;
    }

    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (_entities.Length >= len) return;

        if (_sources.Length != _entities.Length || _transforms.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _sources, newSize);
        Array.Resize(ref _transforms, newSize);
        Array.Resize(ref _boxes, newSize);

        Console.WriteLine("EntityCoreStore resize");
    }
}