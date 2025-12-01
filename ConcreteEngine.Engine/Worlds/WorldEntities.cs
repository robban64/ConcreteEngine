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

    internal EntityStore<ModelComponent> Models { get; }
    internal EntityStore<AnimationComponent> Animations { get; }
    internal EntityStore<ParticleComponent> Particles { get; }
    internal EntityStore<BoxComponent> BoundingBoxes { get; }

    private readonly List<IEntityStore> _storeList;

    internal WorldEntities()
    {
        Core = new EntityCoreStore();
        Models = GenericStores<ModelComponent>.CreateStore();
        Animations = GenericStores<AnimationComponent>.CreateStore();
        Particles = GenericStores<ParticleComponent>.CreateStore();
        BoundingBoxes = GenericStores<BoxComponent>.CreateStore();
        _storeList = [Models, Animations, Particles, BoundingBoxes];
    }

    public int EntityCount => Core.Count;

    internal void AttachRender(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    private EntityId CreateCoreEntity(int id, RenderSourceType sourceType, in MaterialTag matTag,
        in Transform transform)
    {
        var matKey = _materialTable.Add(in matTag);
        return Core.AddEntity(new RenderSourceComponent(id, matKey, sourceType), in transform);
    }

    public EntityId CreateModelEntity(ModelId model, int drawCount, in MaterialTag tag, in Transform transform,
        in BoundingBox boundingBox)
    {
        var entityId = CreateCoreEntity(model, RenderSourceType.ModelAsset, in tag, in transform);
        Models.Add(entityId, new ModelComponent(model, drawCount));
        BoundingBoxes.Add(entityId, new BoxComponent(in boundingBox));
        return entityId;
    }

    public void AddComponent<T>(EntityId entityId, in T component) where T : unmanaged
    {
        GenericStores<T>.Store.Add(entityId, component);
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