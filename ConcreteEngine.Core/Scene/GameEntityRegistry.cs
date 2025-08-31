using System.Collections.Immutable;

namespace ConcreteEngine.Core.Scene;

public sealed class GameEntityRegistry<T> where T : struct
{
    private readonly Dictionary<GameEntityId, int> _map;
    private T[] _data;
    private GameEntityId[] _entities;
    
    private int _idx;

    public GameEntityRegistry(int initialCapacity = 16)
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
            ArgumentOutOfRangeException.ThrowIfNotEqual(_data.Length, _entities.Length);
            var newSize = Math.Max(_data.Length * 2, 8);
            Array.Resize(ref _data, newSize);
            Array.Resize(ref _entities, newSize);
        }

        _map[e] = _idx;
        _entities[_idx] = e;
        _data[_idx] = value;
        return ref _data[_idx++];
    }
    
    public Span<T> AsSpan() => _data.AsSpan(0, _idx);
    public EntityEnumerator<T> GetEnumerator() => new(this);

    public struct EntityEnumerator<T1>(GameEntityRegistry<T1> r)
        where T1 : struct
    {
        private int _i = -1;

        public bool MoveNext() => ++_i < r.Count;
        public Item Current => new Item(r.EntityByIndex(_i), _i, r);

        public readonly ref struct Item(GameEntityId e, int idx, GameEntityRegistry<T1> r)
        {
            public readonly GameEntityId Entity = e;
            public readonly int Index = idx;
            public ref T1 Value => ref r.ByIndex(Index);
        }

        public EntityEnumerator<T1> GetEnumerator() => this;
    }
    
    
    private static int BinarySearchEntity<T2>(ReadOnlySpan<GameEntityId> collection, GameEntityId entity)
    {
        var id = entity.Id;

        int lo = 0, hi = collection.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            int midKey = collection[mid].Id;
            if (midKey == id) return mid;
            if (midKey < id) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }
}