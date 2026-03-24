using System.Runtime.CompilerServices;
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
    private int FindIndex(GameEntityId entity) => SortMethod.BinarySearch(_entities.AsSpan(0, Count), entity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Add(GameEntityId entity, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        var index = AllocateNext();

        _entities[index] = entity;
        _data[index] = value;
        ref var data = ref _data[index];
        foreach (var it in _listeners)
            it.ComponentAdded(entity, ref data);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Remove(GameEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));

        var idx = FindIndex(entity);
        if (idx == -1) throw new ArgumentOutOfRangeException(nameof(entity));

        ref var data = ref _data[idx];
        foreach (var it in _listeners)
            it.ComponentRemoved(entity, ref data);

        _entities[idx] = default;
        data = default;
        FreeEntity(idx);
    }

    public void BindListener(IGameComponentListener<T> listener) => _listeners.Add(listener);
    public void UnbindListener(IGameComponentListener<T> listener) => _listeners.Remove(listener);


    protected override void Resize(int newSize)
    {
        if (_data.Length != _entities.Length)
            throw new InvalidOperationException();

        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _data, newSize);
    }
}