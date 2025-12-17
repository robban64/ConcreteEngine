using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Entities.Resources;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal interface IEntityStore
{
    void EndTick();
}

internal sealed class EntityStore<T> : IEntityStore where T : unmanaged
{
    private T[] _data;
    private EntityId[] _entities;
    private readonly Stack<int> _free = [];

    public int Count { get; private set; }
    public bool IsDirty { get; internal set; }

    public int ActiveCount => Count - _free.Count;
    
    public EntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new EntityId[initialCapacity];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityId GetHandle(int i) => _entities[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(EntityId entity) => ref _data[FindIndex(entity)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];


    public Span<EntityId> GetEntitySpan() => _entities.AsSpan(0, Count);
    public Span<T> GetComponentSpan() => _data.AsSpan(0, Count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(EntityId entity) => EntityUtility.BinarySearchEntity(GetEntitySpan(), entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(EntityId entity)
    {
        var index = FindIndex(entity);
        return (uint)index < (uint)Count && _entities[index] == entity;
    }

    public bool TryGet(EntityId entity, out T value)
    {
        var id = FindIndex(entity);
        if (id >= Count || id < 0)
        {
            value = default;
            return false;
        }

        value = _data[id];
        return true;
    }

    public T GetOrDefault(EntityId entity)
    {
        var index = FindIndex(entity);
        if (index >= 0 && index < _data.Length) return _data[index];
        return default;
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


    // public EntityEnumerator<T> GetEnumerator() => new(this);
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
}