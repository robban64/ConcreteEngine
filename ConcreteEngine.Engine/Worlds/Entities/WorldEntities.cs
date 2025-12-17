using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Entities.Resources;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal sealed class WorldEntities
{
    public const int DefaultEntityCapacity = 1024;

    private static readonly List<IEntityStore> StoreList = [];
    private static EntityCoreStore _coreStore = null!;

    public int EntityCount => _coreStore.ActiveCount;

    internal WorldEntities()
    {
        if (_coreStore is not null || StoreList.Count > 0)
            throw new InvalidOperationException("WorldEntities already initialized");

        _coreStore = new EntityCoreStore(DefaultEntityCapacity);
        GenericStores<ModelComponent>.CreateStore(DefaultEntityCapacity);
        GenericStores<AnimationComponent>.CreateStore(64);
        GenericStores<ParticleComponent>.CreateStore(16);
        GenericStores<SelectionComponent>.CreateStore(16);
        GenericStores<DebugBoundsComponent>.CreateStore(16);
    }

    public EntityCoreStore Core => _coreStore;

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
        foreach (var store in StoreList) store.EndTick();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityEnumerator<T> Query<T>() where T : unmanaged, IEntityComponent => new(GenericStores<T>.Store);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityCoreEnumerator CoreQuery() => new(_coreStore.GetCoreView());

    internal EntityStore<T> GetStore<T>() where T : unmanaged, IEntityComponent => GenericStores<T>.Store;

    private static class GenericStores<T> where T : unmanaged, IEntityComponent
    {
        public static EntityStore<T> Store = null!;

        public static EntityStore<T> CreateStore(int cap)
        {
            var store = new EntityStore<T>(cap);
            StoreList.Add(store);
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