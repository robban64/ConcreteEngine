using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.Integration;

namespace ConcreteEngine.Core.Engine.ECS;

internal interface IGameEntityStore;

public sealed class GameEntityStore<T> : EcsStore, IGameEntityStore where T : unmanaged, IGameComponent<T>
{
    private T[] _data;
    private GameEntityId[] _entities;
    private readonly List<IGameComponentListener<T>> _listeners = new(32);

    public GameEntityStore(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 16);
        _data = new T[initialCapacity];
        _entities = new GameEntityId[initialCapacity];
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.Game;

    public override Span<int> GetRawEntities() => MemoryMarshal.Cast<GameEntityId, int>(_entities.AsSpan(0, Count));

    internal override void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
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
    private int FindIndex(GameEntityId entity) => SearchMethod.BinarySearch(_entities.AsSpan(0, Count), entity);

    public bool Add(GameEntityId entity, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        if (Has(entity)) return false;

        var index = AllocateNext();

        _entities[index] = entity;
        _data[index] = value;
        ref var data = ref _data[index];
        foreach (var it in _listeners)
            it.ComponentAdded(entity.Id, ref data);

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Remove(GameEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

        var index = FindIndex(entity);
        if (index == -1) return false;

        ref var data = ref _data[index];
        foreach (var it in _listeners)
            it.ComponentRemoved(entity.Id, ref data);

        FreeEntity(index);
        data = default;

        return true;
    }

    public void BindListener(IGameComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IGameComponentListener<T> listener) => _listeners.Remove(listener);


    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void Resize(int newSize)
    {
        if (_data.Length != _entities.Length)
            throw new InvalidOperationException();

        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
    }
}