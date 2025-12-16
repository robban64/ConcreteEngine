using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal interface IEntityStore
{
    void EndTick();
}

internal sealed class EntityStore<T> : IEntityStore where T : unmanaged
{
    private T[] _data;
    private EntityHandle[] _entities;
    //private Stack<int> _free = [];

    public int Count { get; private set; }
    public bool IsDirty { get; internal set; }

    internal int Low { get; private set; }
    internal int High { get; private set; }


    public EntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _data = new T[initialCapacity];
        _entities = new EntityHandle[initialCapacity];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityHandle GetHandle(int i) => _entities[i];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(EntityHandle e) => ref _data[FindIndex(e)];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetByIndex(int i) => ref _data[i];
    
    
    public Span<EntityHandle> GetEntitySpan() => _entities.AsSpan(0, Count);
    public Span<T> GetComponentSpan() => _data.AsSpan(0, Count);
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(EntityHandle e) => EntityUtility.BinarySearchEntity(GetEntitySpan(), e);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(EntityHandle e)
    {
        var index = FindIndex(e);
        return (uint)index < (uint)Count && _entities[index] == e;
    }

    public bool TryGet(EntityHandle e, out T value)
    {
        var id = FindIndex(e);
        if (id >= Count || id < 0)
        {
            value = default;
            return false;
        }

        value = _data[id];
        return true;
    }

    public T GetOrDefault(EntityHandle e)
    {
        var index = FindIndex(e);
        if (index >= 0 && index < _data.Length) return _data[index];
        return default;
    }



    public void Add(EntityHandle e, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Value, nameof(e));

        EnsureCapacity(1);
        _entities[Count] = e;
        _data[Count] = value;
        IsDirty = true;
        Count++;
    }

    //TODO
    public void Remove(EntityHandle e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Value, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Value, Count, nameof(e));

        var idx = e - 1;
        _entities[idx] = default;
        _data[idx] = default;
        //_free.Push(idx);
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