#region

using System.Diagnostics;
using ConcreteEngine.Core.Scene.Entities;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class EntityStore<T> where T : unmanaged
{
    private readonly Dictionary<EntityId, int> _map;
    private T[] _data;
    private EntityId[] _entities;

    private int _idx = 0;

    public bool IsDirty { get; set; }

    public EntityStore(int initialCapacity = 16)
    {
        _map = new Dictionary<EntityId, int>(initialCapacity);
        _data = new T[initialCapacity];
        _entities = new EntityId[initialCapacity];
    }

    public int Count => _idx;

    public bool Has(EntityId e) => _map.ContainsKey(e);

    public ref T Get(EntityId e) => ref _data[_map[e]];

    public ref T ByIndex(int i) => ref _data[i];

    public EntityId EntityByIndex(int i) => _entities[i];


    public ref T Add(EntityId e, T value)
    {
        if (_idx == _data.Length)
        {
            Debug.Assert(_data.Length == _entities.Length);
            var newSize = Math.Max(_data.Length * 2, 8);
            Array.Resize(ref _data, newSize);
            Array.Resize(ref _entities, newSize);
        }

        IsDirty = true;

        _map[e] = _idx;
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

    public EntityEnumerator<T, T2> View2<T2>(EntityStore<T2> r2)
        where T2 : unmanaged =>
        new(this, r2);

    public EntityEnumerator<T, T2, T3> View3<T2, T3>(EntityStore<T2> r2,
        EntityStore<T3> r3)
        where T2 : unmanaged
        where T3 : unmanaged =>
        new(this, r2, r3);


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