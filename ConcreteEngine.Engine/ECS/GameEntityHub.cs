using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.ECS.Enumerators;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.ECS;

public sealed class GameEntityHub
{
    private const int DefaultCapacity = 128;

    private static GameEntityId MakeGameEntity() => new(++_count, 1);
    private static int _count;

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
        GenericStores<RenderLink>.CreateStore(DefaultCapacity);
        GenericStores<VisibilityComponent>.CreateStore(DefaultCapacity);
        GenericStores<TransformComponent>.CreateStore(DefaultCapacity);
        GenericStores<BoundingBoxComponent>.CreateStore(DefaultCapacity);
        GenericStores<AnimationComponent>.CreateStore(64);
        GenericStores<TagComponent>.CreateStore(32);
        GenericStores<ParticleRefComponent>.CreateStore(32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(GameEntityId e)
    {
        var index = e.Index;
        return index > 0 && index < _count && _entities[index] == e;
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
        GenericStores<T>.Store.Add(entity, component);
    }

    public void RemoveComponent<T>(GameEntityId entity) where T : unmanaged, IGameComponent<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Id, nameof(entity.Id));
        GenericStores<T>.Store.Remove(entity);
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


    public GameQuery<T1>.EntityEnumerator Query<T1>() where T1 : unmanaged, IGameComponent<T1>
        => new(GenericStores<T1>.Store);

    public GameQuery<T1, T2>.EntityEnumerator Query<T1, T2>()
        where T1 : unmanaged, IGameComponent<T1> where T2 : unmanaged, IGameComponent<T2> =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store);

    public GameEntityStore<T> GetStore<T>() where T : unmanaged, IGameComponent<T> => GenericStores<T>.Store;

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

    private static class GenericStores<T> where T : unmanaged, IGameComponent<T>
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