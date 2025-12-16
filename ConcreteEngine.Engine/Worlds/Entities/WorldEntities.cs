using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Entities.Resources;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal sealed class WorldEntities
{
    public const int DefaultEntityCapacity = 1024;
    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    private readonly List<IEntityStore> _storeList;
    private readonly EntityStore<AnimationComponent> _animations;
    private readonly EntityStore<ParticleComponent> _particles;

    private readonly EntityStore<SelectionComponent> _selections;
    private readonly EntityStore<DebugBoundsComponent> _debugBounds;

    private static EntityCoreStore _coreStore = null!;

    internal WorldEntities()
    {
        _coreStore = new EntityCoreStore(DefaultEntityCapacity);
        _animations = GenericStores<AnimationComponent>.CreateStore(64);
        _particles = GenericStores<ParticleComponent>.CreateStore(16);

        _selections = GenericStores<SelectionComponent>.CreateStore(16);
        _debugBounds = GenericStores<DebugBoundsComponent>.CreateStore(16);

        _storeList = [_animations, _particles, _selections, _debugBounds];
    }


    public int EntityCount => _coreStore.ActiveCount;
    public EntityCoreStore Core => _coreStore;
    public EntityStore<AnimationComponent> Animations => _animations;
    public EntityStore<ParticleComponent> Particles => _particles;


    public void Attach(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public EntityId AddEntity(in CoreComponentBundle data) => Core.AddEntity(in data);

    public void AddComponent<T>(EntityId entity, in T component) where T : unmanaged, IEntityComponent
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity.Value));
        GenericStores<T>.Store.Add(entity, component);
    }

    public void RemoveComponent<T>(EntityId entity) where T : unmanaged, IEntityComponent
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity.Value));
        GenericStores<T>.Store.Remove(entity);
    }

    public void EndTick()
    {
        foreach (var store in _storeList) store.EndTick();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityEnumerator<T> Query<T>() where T : unmanaged, IEntityComponent => new(GenericStores<T>.Store);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCoreEnumerator CoreQuery() => new(_coreStore.GetCoreView());

    public EntityStore<T> GetStore<T>() where T : unmanaged, IEntityComponent => GenericStores<T>.Store;

    private static class GenericStores<T> where T : unmanaged, IEntityComponent
    {
        public static EntityStore<T> Store = null!;

        public static EntityStore<T> CreateStore(int cap)
        {
            var store = new EntityStore<T>(cap);
            Store = store;
            return store;
        }
    }

/*
    internal EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store);

    internal EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store, GenericStores<T3>.Store);
        */
}