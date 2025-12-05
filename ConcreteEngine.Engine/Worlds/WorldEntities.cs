#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldEntities
{
    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    internal EntityCoreStore Core => _coreStore;

    internal EntityStore<AnimationComponent> Animations { get; }
    internal EntityStore<ParticleComponent> Particles { get; }

    private readonly List<IEntityStore> _storeList;

    internal WorldEntities()
    {
        _coreStore = new EntityCoreStore(1024);
        Animations = GenericStores<AnimationComponent>.CreateStore(64);
        Particles = GenericStores<ParticleComponent>.CreateStore(64);
        _storeList = [Animations, Particles];
    }

    public int EntityCount => Core.Count;

    internal void AttachRender(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
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
        var entity = Core.AddEntity(source, Transform.Identity, default, out var index);
        Particles.Add(entity, index,component);
        return entity;
    }

    public void AddComponent<T>(EntityId entityId, in T component) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityId.Id, nameof(entityId));
        GenericStores<T>.Store.Add(entityId, Core.GetIndexByEntity(entityId), component);
    }

    internal void EndTick()
    {
        foreach (var store in _storeList)
            store.EndTick();
    }
    
    internal EntityEnumerator<T> Query<T>() where T : unmanaged => new(GenericStores<T>.Store);

    internal EntityCoreEnumerator CoreQuery()  => new(_coreStore);

    private static EntityCoreStore _coreStore = null!;
    //internal static EntityCoreStore GetCoreStore() => _coreStore;
   // internal static EntityCoreEnumerator CoreQuery()  => new(_coreStore);


   // internal static EntityStore<T> GetStore<T>() where T : unmanaged => GenericStores<T>.Store;
    //internal static EntityEnumerator<T1> Query<T1>() where T1 : unmanaged => new(GenericStores<T1>.Store);
    
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