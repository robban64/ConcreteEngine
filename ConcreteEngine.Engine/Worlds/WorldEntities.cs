#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
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
    internal EntityStore<BoxComponent> BoundingBoxes { get; }
    internal EntityStore<AnimationComponent> Animations { get; }

    private readonly List<IEntityStore> _storeList;

    internal WorldEntities()
    {
        Core = new EntityCoreStore();
        BoundingBoxes = GenericStores<BoxComponent>.CreateStore();
        Animations = GenericStores<AnimationComponent>.CreateStore();
        _storeList = [BoundingBoxes,  Animations];
    }

    public int EntityCount => Core.Count;

    internal void AttachRender(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public EntityId CreateModelEntity(ModelId model, int drawCount, MaterialTag tag, in Transform transform,
        in BoundingBox boundingBox)
    {
        var matKey = _materialTable.Add(tag);
        var entityId = Core.AddEntity(new ModelComponent(model, drawCount, matKey), in transform);
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

