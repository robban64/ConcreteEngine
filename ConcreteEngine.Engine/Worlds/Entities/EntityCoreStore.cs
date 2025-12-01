using System.Diagnostics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

public sealed class EntityCoreStore
{
    private const int DefaultCapacity = 256;

    //private int _entityIdx = 0;
    private EntityId Create() => new(++_idx);
    private int _idx = 0;

    private int[] _sparse = new int[DefaultCapacity];
    private EntityId[] _entities = new EntityId[DefaultCapacity];
    private RenderSourceComponent[] _sources = new RenderSourceComponent[DefaultCapacity];
    private Transform[] _transforms = new Transform[DefaultCapacity];

    private Stack<int> _free = [];

    public int Count => int.Max(0, _idx - 1 - _free.Count);
    public bool IsDirty { get; private set; }

    internal EntityCoreStore()
    {
    }

    public EntityView GetEntityView(EntityId e) => new(e, ref _sources[_sparse[e]], ref _transforms[_sparse[e]]);
    public ref RenderSourceComponent GetModelById(EntityId e) => ref _sources[_sparse[e]];
    public ref Transform GetTransformById(EntityId e) => ref _transforms[_sparse[e]];

    public Span<EntityId> GetEntitySpan() => _entities.AsSpan(0, _idx);
    public Span<RenderSourceComponent> GetModelSpan() => _sources.AsSpan(0, _idx);
    public Span<Transform> GetTransformSpan() => _transforms.AsSpan(0, _idx);

    public EntitiesCoreView GetCoreView() =>
        new(_entities.AsSpan(0, _idx), _sources.AsSpan(0, _idx), _transforms.AsSpan(0, _idx));

    public bool Has(EntityId e)
    {
        var index = _sparse[e - 1];
        return (uint)index < (uint)_idx && _entities[index] == e;
    }

    public EntityId AddEntity(RenderSourceComponent model, in Transform transform)
    {
        //var idx = _free.Count > 0 ? _free.Pop() : Create();
        var idx = _idx;
        var e = Create();
        EnsureCapacity(1);

        _sparse[e - 1] = idx;
        _entities[idx] = e;
        _sources[idx] = model;
        _transforms[idx] = transform;

        IsDirty = true;
        return e;
    }

    public void Remove(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _idx, nameof(e));
        var idx = e.Id - 1;
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
        Console.WriteLine("EntityCoreStore resize");
    }
}