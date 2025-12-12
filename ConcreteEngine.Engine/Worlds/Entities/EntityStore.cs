#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Engine.Worlds.Entities;

internal interface IEntityStore
{
    int Count { get; }
    bool IsDirty { get; }
    void Remove(EntityId id);
    void EndTick();
}

internal sealed class EntityStore<T> : IEntityStore where T : unmanaged
{
    private T[] _data;
    private int[] _coreIndices;
    private EntityId[] _entities;

    //private Stack<int> _free = [];

    private int _idx = 0;

    public int Low { get; private set; }
    public int High { get; private set; }


    public EntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 32);
        _data = new T[initialCapacity];
        _entities = new EntityId[initialCapacity];
        _coreIndices = new int[initialCapacity];

        _coreIndices.AsSpan().Fill(-1);
    }

    public int Count => _idx;
    public bool IsDirty { get; internal set; }


    public Span<EntityId> GetEntitySpan() => _entities.AsSpan(0, _idx);
    public Span<T> GetComponentSpan() => _data.AsSpan(0, _idx);
    public Span<int> GetCoreIndexSpan() => _coreIndices.AsSpan(0, _idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(EntityId e) => EntityUtility.BinarySearchEntity(GetEntitySpan(), e);


    public bool Has(EntityId e)
    {
        var index = FindIndex(e);
        return (uint)index < (uint)_idx && _entities[index] == e;
    }

    public bool Has(EntityId e, int coreIndex)
    {
        if (coreIndex < Low || coreIndex > High) return false;
        var index = FindIndex(e);
        return (uint)index < (uint)_idx && _entities[index] == e;
    }

    
    public bool TryGetById(EntityId e, out T value)
    {
        var id = FindIndex(e);
        if (id >= _idx || id < 0)
        {
            value = default;
            return false;
        }

        value = _data[id];
        return true;
    }

    public T GetByIdOrDefault(EntityId e)
    {
        var index = FindIndex(e);
        if (index >= 0 && index < _data.Length) return _data[index];
        return default;
    }
    
    public ref T GetById(EntityId e) => ref _data[FindIndex(e)];
    public ref T GetByIndex(int i) => ref _data[i];
    public EntityId GetEntityId(int i) => _entities[i];
    public int GetCoreIndex(int i) =>  _coreIndices[i];

    public T GetByIndexOrDefault(int index)
    {
        if (index >= 0 && index < _data.Length) return _data[index];
        return default;
    }


    public void Add(EntityId e, int index, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        Low = _idx == 0 ? index : int.Min(Low, index);
        High = int.Max(High, index);

        EnsureCapacity(1);
        _entities[_idx] = e;
        _data[_idx] = value;
        _coreIndices[_idx] = index;
        IsDirty = true;
        _idx++;
    }

    //TODO
    public void Remove(EntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _idx, nameof(e));

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
        var len = _idx + amount;
        if (_entities.Length >= len) return;

        if (_data.Length != _entities.Length)
        {
            throw new InvalidOperationException();
        }

        var prevLen = _coreIndices.Length;

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
        Array.Resize(ref _coreIndices, newSize);

        _coreIndices.AsSpan(prevLen).Fill(-1);

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