#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

internal sealed class EntityCoreStore
{
    //private int _entityIdx = 0;
    private EntityId Create() => new(++_idx);
    private int _idx = 0;

    private EntityId[] _entities;
    private RenderSourceComponent[] _sources;
    private Transform[] _transforms;
    private BoxComponent[] _boxes;

    private Stack<int> _free = [];

    public int Count => int.Max(0, _idx - 1 - _free.Count);
    public int TotalCount => _idx;

    public bool IsDirty { get; private set; }


    internal EntityCoreStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _entities = new EntityId[initialCapacity];
        _sources = new RenderSourceComponent[initialCapacity];
        _transforms = new Transform[initialCapacity];
        _boxes = new BoxComponent[initialCapacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref RenderSourceComponent GetSourceById(EntityId e) => ref _sources[e - 1];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Transform GetTransformById(EntityId e) => ref _transforms[e - 1];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BoxComponent GetBoxById(EntityId e) => ref _boxes[e - 1];

    public Span<EntityId> GetEntitySpan() => _entities.AsSpan(0, _idx);
    public Span<Transform> GetTransformSpan() => _transforms.AsSpan(0, _idx);
    public Span<BoxComponent> GetBoxSpan() => _boxes.AsSpan(0, _idx);

    public EntityView GetEntityView(EntityId e)
    {
        var idx = e - 1;
        if ((uint)idx >= _entities.Length) throw new IndexOutOfRangeException();
        return new EntityView(e, ref _sources[idx], ref _transforms[idx], ref _boxes[idx]);
    }

    public EntitiesCoreView GetCoreView() =>
        new(_entities.AsSpan(0, _idx), _sources.AsSpan(0, _idx), _transforms.AsSpan(0, _idx), _boxes.AsSpan(0, _idx));



    public EntityId AddEntity(RenderSourceComponent renderSource, in Transform transform, in BoundingBox bounds,
        out int index)
    {
        //var idx = _free.Count > 0 ? _free.Pop() : Create();
        index = _idx;
        var e = Create();
        EnsureCapacity(1);

        _entities[index] = e;
        _sources[index] = renderSource;
        _transforms[index] = transform;
        _boxes[index] = bounds;

        IsDirty = true;
        return e;
    }

    public void Remove(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _idx, nameof(e));
    }

    internal void EndTick()
    {
        IsDirty = false;
    }

    private void EnsureCapacity(int amount)
    {
        var len = _idx + amount;
        if (_entities.Length >= len) return;

        if (_sources.Length != _entities.Length || _transforms.Length != _entities.Length )
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