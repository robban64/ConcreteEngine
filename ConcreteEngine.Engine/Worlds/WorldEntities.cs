using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldEntities
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
    internal EntityCoreStore Core => _coreStore;
    internal EntityStore<AnimationComponent> Animations => _animations;
    internal EntityStore<ParticleComponent> Particles => _particles;

    internal ReadOnlySpan<EntityResolverEntry> ResolvedEntitySpan => _renderResolver.Entities;

    internal void Attach(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public void AddComponent<T>(EntityId entity, in T component) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Handle, nameof(entity.Handle));
        GenericStores<T>.Store.Add(entity, component);
    }

    internal void ApplyRenderResolverFor(EntityHandle entityHandle, RenderResolver resolver)
    {
        _renderResolver.AddResolver(entityHandle, resolver);
    }

    internal void RemoveRenderResolverFor(EntityHandle entityHandle) => _renderResolver.RemoveResolver(entityHandle);

    internal void EndTick()
    {
        foreach (var store in _storeList)
            store.EndTick();
    }

    public EntityId CreateModelEntity(ModelId id, int draw, in MaterialTag mat, in Transform tran, in BoundingBox box)
    {
        var matKey = _materialTable.Add(in mat);
        var source = new RenderSourceComponent(id, draw, matKey, RenderSourceKind.Model);
        var core = new CoreComponent(source, in tran, box);
        return Core.AddEntity(EntityKind.Model, in core);
    }

    public EntityHandle CreateParticleEntity(ParticleEmitter emitter, ParticleComponent component)
    {
        var source = new RenderSourceComponent(emitter.Model, 4, emitter.MaterialKey, RenderSourceKind.Particle);
        var core = new CoreComponent(in source, in Transform.Identity, ParticleComponent.DefaultParticleBounds);
        var entity = Core.AddEntity(EntityKind.Particle, in core);
        Particles.Add(entity, component);
        return entity;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityEnumerator<T> Query<T>() where T : unmanaged => new(GenericStores<T>.Store);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityCoreEnumerator CoreQuery() => new(_coreStore.GetCoreView());

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