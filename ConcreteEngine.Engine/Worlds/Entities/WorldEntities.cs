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
    private readonly EntityRenderResolver _renderResolver;
    private readonly EntityStore<AnimationComponent> _animations;
    private readonly EntityStore<ParticleComponent> _particles;

    private static EntityCoreStore _coreStore = null!;

    internal WorldEntities()
    {
        _coreStore = new EntityCoreStore(DefaultEntityCapacity);
        _animations = GenericStores<AnimationComponent>.CreateStore(64);
        _particles = GenericStores<ParticleComponent>.CreateStore(64);
        _storeList = [Animations, Particles];

        _renderResolver = new EntityRenderResolver();
    }


    public int EntityCount => _coreStore.ActiveCount;
    public EntityCoreStore Core => _coreStore;
    public EntityStore<AnimationComponent> Animations => _animations;
    public EntityStore<ParticleComponent> Particles => _particles;

    public ReadOnlySpan<EntityResolverEntry> ResolvedEntitySpan => _renderResolver.Entities;

    public void Attach(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public void AddComponent<T>(EntityId entity, in T component) where T : unmanaged, IEntityComponent
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity.Value));
        GenericStores<T>.Store.Add(entity, component);
    }

    public void ApplyRenderResolverFor(EntityId entity, RenderResolver resolver)
    {
        _renderResolver.AddResolver(entity, resolver);
    }

    public void RemoveRenderResolverFor(EntityId entity) => _renderResolver.RemoveResolver(entity);

    public void EndTick()
    {
        foreach (var store in _storeList)
            store.EndTick();
    }

    public EntityId AddEntity(in CoreComponentBundle data) => Core.AddEntity(in data);

    public void AddEntities(ReadOnlySpan<CoreComponentBundle> components, Span<EntityId> result) =>
        Core.AddEntities(components, result);


    public EntityId CreateModelEntity(ModelId id, in MaterialTag mat, in Transform tran, in BoundingBox box)
    {
        var matKey = _materialTable.Add(in mat);
        var source = new SourceComponent(id, matKey, EntitySourceKind.Model);
        var core = new CoreComponentBundle(in source, in tran, box);
        return Core.AddEntity(in core);
    }

    public EntityId CreateParticleEntity(ParticleEmitter emitter, ParticleComponent component)
    {
        var source = new SourceComponent(emitter.Model, emitter.MaterialKey, EntitySourceKind.Particle);
        var core = new CoreComponentBundle(in source, in Transform.Identity, ParticleComponent.DefaultParticleBounds);
        var entity = Core.AddEntity(in core);
        Particles.Add(entity, component);
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityEnumerator<T> Query<T>() where T : unmanaged, IEntityComponent => new(GenericStores<T>.Store);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCoreEnumerator CoreQuery() => new(_coreStore.GetCoreView());


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