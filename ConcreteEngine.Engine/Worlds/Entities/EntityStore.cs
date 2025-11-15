#region

using System.Diagnostics;
using ConcreteEngine.Common.Collections;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

public sealed class EntityStore<T> where T : unmanaged
{
    private int[] _sparse;
    private T[] _data;
    private EntityId[] _entities;

    private int _idx = 0;

    public bool IsDirty { get; set; }

    public EntityStore(int initialCapacity = 128)
    {
        _sparse = new int[initialCapacity];
        _data = new T[initialCapacity];
        _entities = new EntityId[initialCapacity];
    }

    public int Count => _idx;

    public bool Has(EntityId e)
    {
        var index = _sparse[e];
        return (uint)index < (uint)_idx && _entities[index] == e;
    }

    public ref T GetById(EntityId e) => ref _data[_sparse[e]];

    public bool TryGetById(EntityId e, out T value)
    {
        if (e.Id >= _idx)
        {
            value = default;
            return false;
        }

        value = _data[_sparse[e]];
        return true;
    }

    public ref T GetByIndex(int i) => ref _data[i];

    public EntityId GetEntityId(int i) => _entities[i];


    public ref T Add(EntityId e, T value)
    {
        if (_idx >= _data.Length)
        {
            Debug.Assert(_data.Length == _entities.Length);
            var newSize = ArrayUtility.CapacityGrowthPow2(Math.Max(_idx, 32));
            Array.Resize(ref _data, newSize);
            Array.Resize(ref _entities, newSize);
            Array.Resize(ref _sparse, newSize);
        }

        IsDirty = true;

        _sparse[e] = _idx;
        _entities[_idx] = e;
        _data[_idx] = value;
        return ref _data[_idx++];
    }

    internal void Cleanup()
    {
        IsDirty = false;
    }

    public Span<EntityId> AsEntitySpan() => _entities.AsSpan(0, _idx);
    public Span<T> AsSpan() => _data.AsSpan(0, _idx);

    public EntityEnumerator<T> GetEnumerator() => new(this);
/*
    public EntityEnumerator<T, T2> Query<T2>(EntityStore<T2> r2)
        where T2 : unmanaged =>
        new(this, r2);

    public EntityEnumerator<T, T2, T3> Query<T2, T3>(EntityStore<T2> r2,
        EntityStore<T3> r3)
        where T2 : unmanaged
        where T3 : unmanaged =>
        new(this, r2, r3);
*/

    private static int BinarySearchEntity<T2>(ReadOnlySpan<EntityId> collection, EntityId entity)
    {
        var id = entity.Id;

        int lo = 0, hi = collection.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            int midKey = collection[mid].Id;
            if (midKey == id) return mid;
            if (midKey < id) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }
}