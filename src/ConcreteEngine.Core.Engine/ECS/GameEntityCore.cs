using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.Integration;
using static ConcreteEngine.Core.Engine.ECS.Ecs.Game;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class GameEntityCore : EcsStore
{
    private GameEntityId[] _entities;
    private readonly List<IEntityListener> _listeners = new(64);

    internal GameEntityCore(int capacity)
    {
        _entities = new GameEntityId[capacity];
    }

    public override int Capacity => _entities.Length;
    public override EcsStoreType StoreType => EcsStoreType.GameCore;

    internal override void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(GameEntityId entity)
    {
        var index = entity.Index();
        return (uint)index < (uint)Count && _entities[index] == entity;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GameEntityId AddEntity()
    {
        var index = AllocateNext();
        var entity = _entities[index] = new GameEntityId(index + 1);
        foreach (var it in _listeners)
            it.EntityAdded(entity, this);

        return entity;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Remove(GameEntityId entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(entity.Id, Count, nameof(entity));

        var index = entity.Index();
        ref var existing = ref _entities[index];
        if (existing != entity) throw new InvalidOperationException();

        _entities[index] = default;

        foreach (var it in _listeners)
            it.EntityRemoved(entity, this);

        FreeEntity(index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddComponent<T>(GameEntityId entity, in T component) where T : unmanaged, IGameComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        Stores<T>.Store.Add(entity, component);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RemoveComponent<T>(GameEntityId entity) where T : unmanaged, IGameComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        Stores<T>.Store.Remove(entity);
    }

    public void BindListener(IEntityListener listener) => _listeners.Add(listener);
    public void UnbindListener(IEntityListener listener) => _listeners.Remove(listener);


    protected override void Resize(int newSize)
    {
        Array.Resize(ref _entities, newSize);
    }
}