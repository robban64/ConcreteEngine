using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Resources;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene.GameEntity;


internal interface IGameEntityStore
{
    void EndTick();
}

public sealed class GameEntityStore <T> : IGameEntityStore where T : unmanaged
{
    private T[] _data;
    private EntityId[] _entities;
    private readonly Dictionary<EntityId, int> _entityToIndex; // temp
    
    private readonly Stack<int> _free = [];
    
    public int Count { get; private set; }
    public bool IsDirty { get; internal set; }

    public int ActiveCount => Count - _free.Count;


    public GameEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new EntityId[initialCapacity];
        _entityToIndex = new Dictionary<EntityId, int>(initialCapacity);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityId GetHandle(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(EntityId entity) => ref _data[FindIndex(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(EntityId entity) => EntityUtility.BinarySearchEntity(_entities.AsSpan(0, Count), entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(EntityId entity)
    {
        var index = FindIndex(entity);
        return (uint)index < (uint)Count && _entities[index] == entity;
    }

    public void Add(EntityId entity, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity));
        if (!_free.TryPop(out var index))
        {
            EnsureCapacity(1);
            index = Count++;
        }

        _entities[index] = entity;
        _data[index] = value;
        _entityToIndex[entity] = index;
        IsDirty = true;
    }

    public void Remove(EntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity));

        var idx = FindIndex(entity);
        if(idx == -1) throw  new ArgumentOutOfRangeException(nameof(entity));
        
        _entities[idx] = default;
        _data[idx] = default;
        _free.Push(idx);
    }

    public void EndTick()
    {
        IsDirty = false;
    }

    private void EnsureCapacity(int amount)
    {
        var len = Count + amount;
        if (_entities.Length >= len) return;

        if (_data.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }


        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
        Logger.LogString(LogScope.World, $"EntityStore: {typeof(T).Name} resized {newSize}", LogLevel.Warn);
    }
}