#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Tables;
using ConcreteEngine.Engine.Worlds.Tables;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldEntities
{
    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    internal EntityCoreStore Core { get; }

    internal EntityStore<RenderSourceComponent> Models { get; }
    internal EntityStore<AnimationComponent> Animations { get; }
    internal EntityStore<ParticleComponent> Particles { get; }

    private readonly List<IEntityStore> _storeList;

    internal WorldEntities()
    {
        Core = new EntityCoreStore();
        Models = GenericStores<RenderSourceComponent>.CreateStore();
        Animations = GenericStores<AnimationComponent>.CreateStore();
        Particles = GenericStores<ParticleComponent>.CreateStore();
        _storeList = [Models, Animations, Particles];
    }

    public int EntityCount => Core.EntityCount;

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
        Models.Add(entityId, index, new RenderSourceComponent(id, draw, matKey));
        return entityId;
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

    internal static EntityStore<T> GetStore<T>() where T : unmanaged => GenericStores<T>.Store;
    internal static EntityEnumerator<T1> Query<T1>() where T1 : unmanaged => new(GenericStores<T1>.Store);

    private static class GenericStores<T> where T : unmanaged
    {
        public static EntityStore<T> Store = null!;

        public static EntityStore<T> CreateStore()
        {
            var store = new EntityStore<T>();
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