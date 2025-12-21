using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Generics;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.ECS;

internal interface IGameEntityStore
{
    void EndTick();
}

public sealed class GameEntityStore<T> : IGameEntityStore where T : unmanaged
{
    private T[] _data;
    private GameEntityId[] _entities;
    private readonly Dictionary<GameEntityId, int> _entityToIndex;

    private readonly Stack<int> _free = [];
    private int _count;
    private bool _isDirty;

    public bool IsDirty => _isDirty;
    public int Count => _count;
    public int ActiveCount => _count - _free.Count;
    public int Capacity => _entities.Length;


    public GameEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new GameEntityId[initialCapacity];
        _entityToIndex = new Dictionary<GameEntityId, int>(initialCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(GameEntityId entity) => FindIndex(entity) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GameEntityId GetEntity(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(GameEntityId entity) => ref _data[FindIndex(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> TryGet(GameEntityId entity)
    {
        var id = FindIndex(entity);
        if ((uint)id >= _data.Length) return ValuePtr<T>.Null;
        return new ValuePtr<T>(ref _data[id]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOrDefault(GameEntityId entity)
    {
        var id = FindIndex(entity);
        if ((uint)id >= _data.Length) return default;
        return _data[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(GameEntityId entity) => SortMethod.BinarySearch(_entities.AsSpan(0, _count), entity);

    public void Add(GameEntityId entity, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (!_free.TryPop(out var index))
        {
            EnsureCapacity(1);
            index = _count++;
        }

        _entityToIndex[entity] = index;
        _entities[index] = entity;
        _data[index] = value;
        _isDirty = true;
    }

    public void Remove(GameEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

        var idx = FindIndex(entity);
        if (idx == -1) throw new ArgumentOutOfRangeException(nameof(entity));

        _entities[idx] = default;
        _data[idx] = default;
        _free.Push(idx);
    }

    public void EndTick()
    {
        _isDirty = true;
    }

    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        if (_entities.Length >= len) return;

        if (_data.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }


        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
        Logger.LogString(LogScope.World, $"GameEntityStore: {typeof(T).Name} resized {newSize}", LogLevel.Warn);
    }
}