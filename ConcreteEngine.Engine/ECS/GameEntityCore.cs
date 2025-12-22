using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Shared.Diagnostics;
using static ConcreteEngine.Engine.ECS.Ecs.Game;

namespace ConcreteEngine.Engine.ECS;

public sealed class GameEntityCore
{
    private static GameEntityId MakeGameEntity() => new(++_count, 1);
    private static int _count;

    private GameEntityId[] _entities;
    private readonly Stack<int> _free = [];

    private bool _isDirty;

    internal GameEntityCore(int capacity)
    {
        _entities = new GameEntityId[capacity];
    }

    public int ActiveCount => _count - _free.Count;
    public int Count => _count;
    public bool IsDirty => _isDirty;

    public void Initialize()
    {
        InvalidOpThrower.ThrowIf(_entities.Length == 0, nameof(_entities));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(GameEntityId e)
    {
        var index = e.Index();
        return (uint)index < (uint)_count && _entities[index] == e;
    }

    public GameEntityId AddEntity()
    {
        if (_free.TryPop(out var index))
        {
            var entity = _entities[index];
            return _entities[index] = new GameEntityId(entity.Id, (ushort)(entity.Gen + 1));
        }

        EnsureCapacity(1);
        return _entities[_count] = MakeGameEntity();
    }

    public void AddComponent<T>(GameEntityId entity, in T component) where T : unmanaged, IGameComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        Stores<T>.Store.Add(entity, component);
    }

    public void RemoveComponent<T>(GameEntityId entity) where T : unmanaged, IGameComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        Stores<T>.Store.Remove(entity);
    }

    public void Remove(GameEntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _count, nameof(e));

        var index = e.Index();
        ref var existing = ref _entities[index];
        if (existing != e) throw new InvalidOperationException();

        _entities[index] = default;
        _free.Push(index);
    }

    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        if (_entities.Length >= len) return;

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);

        Logger.LogString(LogScope.World, $"GameEntities: resized {newSize}", LogLevel.Warn);
    }
}