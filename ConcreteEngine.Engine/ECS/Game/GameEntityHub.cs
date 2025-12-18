using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.ECS.Game;

public sealed class GameEntityHub
{
    private const int DefaultCapacity = 128;

    private static GameEntityId MakeGameEntity() => new(++_count, 1);
    private static int _count = 0;

    private GameEntityId[] _entities;
    private readonly Stack<int> _free = [];

    private bool _isDirty;

    public int ActiveCount => _count - _free.Count;
    public int Count => _count;
    public bool IsDirty => _isDirty;

    internal GameEntityHub()
    {
        if (_count > 0 || StaticStores.All.Count > 0)
            throw new InvalidOperationException("GameEntityHub already initialized");

        _entities = new GameEntityId[DefaultCapacity];
    }

    private GameEntityId AddEntity()
    {
        if (_free.TryPop(out var index))
        {
            var entity = _entities[index];
            return _entities[index] = new GameEntityId(entity.Id, (ushort)(entity.Gen + 1));
        }

        EnsureCapacity(1);
        
        index = _count;
        return _entities[index] = MakeGameEntity();
    }

    public void Remove(GameEntityId e)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Id, nameof(e));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(e.Id, _count, nameof(e));

        var index = e.Index;
        ref var existing = ref _entities[index];
        if (existing != e) throw new InvalidOperationException();

        _entities[index] = default;
        _free.Push(index);
    }

    public bool HasEntity(GameEntityId e)
    {
        var index = e.Index;
        return (uint)index < (uint)Count && _entities[index] == e;
    }


    public GameEntityEnumerator<T1> Query<T1>() where T1 : unmanaged, IEntityComponent
        => new(GenericStores<T1>.Store);

    public GameEntityEnumerator<T1, T2> Query<T1, T2>()
        where T1 : unmanaged, IEntityComponent where T2 : unmanaged, IEntityComponent =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store);


    private void EnsureCapacity(int amount)
    {
        var len = _count + amount;
        if (_entities.Length >= len) return;

        var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
        Array.Resize(ref _entities, newSize);

        Logger.LogString(LogScope.World, $"GameEntities: resized {newSize}", LogLevel.Warn);
    }

    private static class StaticStores
    {
        public static readonly List<IGameEntityStore> All = [];
    }

    private static class GenericStores<T> where T : unmanaged, IEntityComponent
    {
        public static GameEntityStore<T> Store = null!;

        public static GameEntityStore<T> CreateStore(int cap)
        {
            var store = new GameEntityStore<T>(cap);
            StaticStores.All.Add(store);
            Store = store;
            return store;
        }
    }
}