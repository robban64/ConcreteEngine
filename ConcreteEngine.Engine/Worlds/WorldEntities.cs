#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldEntities
{
    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    private readonly List<IEntityStore> _storeList;
    private readonly EntityRenderResolver _renderResolver;
    private readonly EntityStore<AnimationComponent> _animations;
    private readonly EntityStore<ParticleComponent> _particles;

    private static EntityCoreStore _coreStore = null!;

    internal WorldEntities()
    {
        _coreStore = new EntityCoreStore(1024);
        _animations = GenericStores<AnimationComponent>.CreateStore(64);
        _particles = GenericStores<ParticleComponent>.CreateStore(64);
        _storeList = [Animations, Particles];

        _renderResolver = new EntityRenderResolver();
    }


    public int EntityCount => _coreStore.Count;
    internal EntityCoreStore Core => _coreStore;
    internal EntityStore<AnimationComponent> Animations => _animations;
    internal EntityStore<ParticleComponent> Particles => _particles;

    internal ReadOnlySpan<EntityResolverEntry> ResolvedEntitySpan => _renderResolver.Entities;

    internal void Attach(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public void AddComponent<T>(EntityId entityId, in T component) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityId.Id, nameof(entityId));
        GenericStores<T>.Store.Add(entityId, entityId - 1, component);
    }

    internal void ApplyRenderResolverFor(EntityId entityId, RenderResolver resolver)
    {
        var isAnimated = Animations.Has(entityId);
        _renderResolver.AddResolver(entityId, resolver, isAnimated);
    }

    internal void RemoveRenderResolverFor(EntityId entityId) => _renderResolver.RemoveResolver(entityId);

    internal void EndTick()
    {
        foreach (var store in _storeList)
            store.EndTick();
    }

    private EntityId CreateCoreEntity(ModelId id, int draw, in MaterialTag matTag, in Transform tran,
        in BoundingBox box, out int index, out MaterialTagKey matKey)
    {
        matKey = _materialTable.Add(in matTag);
        return Core.AddEntity(new RenderSourceComponent(id, draw, matKey), in tran, in box, out index);
    }

    public EntityId CreateModelEntity(ModelId id, int draw, in MaterialTag mat, in Transform tran, in BoundingBox box)
    {
        var entityId = CreateCoreEntity(id, draw, in mat, in tran, in box, out var index, out var matKey);
        return entityId;
    }

    public EntityId CreateParticleEntity(MeshId mesh, ParticleComponent component)
    {
        var source = new RenderSourceComponent(ModelId.Ignore, 4, MaterialTagKey.Ignore, RenderSourceKind.Particle);
        var entity = Core.AddEntity(source, Transform.Identity, BoundingBox.Identity, out var index);
        Particles.Add(entity, index, component);
        return entity;
    }


    internal EntityEnumerator<T> Query<T>() where T : unmanaged => new(GenericStores<T>.Store);

    internal EntityCoreEnumerator CoreQuery() => new(_coreStore);

    private static class GenericStores<T> where T : unmanaged
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