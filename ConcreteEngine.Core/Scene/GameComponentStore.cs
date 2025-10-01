#region

using System.Diagnostics;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class GameComponentStore<T> where T : unmanaged
{
    private readonly Dictionary<GameEntityId, int> _map;
    private T[] _data;
    private GameEntityId[] _entities;

    private int _idx = 0;

    public bool IsDirty { get; set; }

    public GameComponentStore(int initialCapacity = 16)
    {
        _map = new Dictionary<GameEntityId, int>(initialCapacity);
        _data = new T[initialCapacity];
        _entities = new GameEntityId[initialCapacity];
    }

    public int Count => _idx;

    public bool Has(GameEntityId e) => _map.ContainsKey(e);

    public ref T Get(GameEntityId e) => ref _data[_map[e]];

    public ref T ByIndex(int i) => ref _data[i];

    public GameEntityId EntityByIndex(int i) => _entities[i];


    public ref T Add(GameEntityId e, T value)
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

    public Span<GameEntityId> AsEntitySpan() => _entities.AsSpan(0, _idx);
    public Span<T> AsSpan() => _data.AsSpan(0, _idx);

    public EntityEnumerator<T> GetEnumerator() => new(this);

    public EntityEnumerator<T, T2> View2<T2>(GameComponentStore<T2> r2)
        where T2 : unmanaged =>
        new(this, r2);

    public EntityEnumerator<T, T2, T3> View3<T2, T3>(GameComponentStore<T2> r2,
        GameComponentStore<T3> r3)
        where T2 : unmanaged
        where T3 : unmanaged =>
        new(this, r2, r3);


    private static int BinarySearchEntity<T2>(ReadOnlySpan<GameEntityId> collection, GameEntityId entity)
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