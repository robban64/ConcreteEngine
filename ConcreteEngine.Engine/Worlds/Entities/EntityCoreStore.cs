using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal sealed class EntityCoreStore
{
    private const int DefaultCapacity = 256;

    //private int _entityIdx = 0;
    private EntityId Create() => new(++_idx);
    private int _idx = 0;

    private int[] _sparse = new int[DefaultCapacity];
    private EntityId[] _entities = new EntityId[DefaultCapacity];
    private RenderSourceComponent[] _sources = new RenderSourceComponent[DefaultCapacity];
    private Transform[] _transforms = new Transform[DefaultCapacity];
    private BoxComponent[] _boxes = new BoxComponent[DefaultCapacity];

    private Stack<int> _free = [];

    public int EntityCount => int.Max(0, _idx - 1 - _free.Count);
    public int TotalCount => _idx;

    public bool IsDirty { get; private set; }


    internal EntityCoreStore()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSparseIndex(EntityId e) => _sparse[e - 1];


    public EntityView GetEntityView(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(e.Id, _sparse.Length, nameof(e));

        var idx = GetSparseIndex(e);
        if ((uint)idx >= _entities.Length) throw new IndexOutOfRangeException();

        return new EntityView(e, ref _sources[idx], ref _transforms[idx], ref _boxes[idx]);
    }

    public ref RenderSourceComponent GetSourceById(EntityId e) => ref _sources[GetSparseIndex(e)];
    public ref Transform GetTransformById(EntityId e) => ref _transforms[GetSparseIndex(e)];
    public ref BoxComponent GetBoundsById(EntityId e) => ref _boxes[GetSparseIndex(e)];


    public Span<EntityId> GetEntitySpan() => _entities.AsSpan(0, _idx);


    public EntitiesCoreView GetCoreView() =>
        new(_entities.AsSpan(0, _idx), _sources.AsSpan(0, _idx), _transforms.AsSpan(0, _idx), _boxes.AsSpan(0, _idx));

    internal int GetIndexByEntity(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        return GetSparseIndex(e);
    }

    public bool Has(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        var index = GetSparseIndex(e);
        return (uint)index < (uint)_idx && _entities[index] == e;
    }

    public EntityId AddEntity(RenderSourceComponent renderSource, in Transform transform, in BoundingBox bounds,
        out int index)
    {
        //var idx = _free.Count > 0 ? _free.Pop() : Create();
        index = _idx;
        var e = Create();
        EnsureCapacity(1);

        _sparse[e - 1] = index;
        _entities[index] = e;
        _sources[index] = renderSource;
        _transforms[index] = transform;
        _boxes[index] = new BoxComponent(in bounds);

        IsDirty = true;
        return e;
    }

    public void Remove(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _idx, nameof(e));
        var idx = GetSparseIndex(e);
    }

    internal void EndTick()
    {
        IsDirty = false;
    }

    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (_entities.Length >= len) return;

        if (_sources.Length != _entities.Length || _transforms.Length != _entities.Length ||
            _sparse.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _sparse, newSize);
        Array.Resize(ref _sources, newSize);
        Array.Resize(ref _transforms, newSize);
        Array.Resize(ref _boxes, newSize);

        Console.WriteLine("EntityCoreStore resize");
    }
}