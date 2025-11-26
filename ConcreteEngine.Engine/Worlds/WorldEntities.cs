#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldEntities
{
    public EntityId Create() => new(_idIdx++);
    private int _idIdx = 1;

    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    public EntityStore<ModelComponent> Models { get; }
    public EntityStore<Transform> Transforms { get; }
    public EntityStore<BoxComponent> BoundingBoxes { get; }
    public EntityStore<AnimationComponent> Animations { get; }

    internal WorldEntities()
    {
        Models = GenericStores<ModelComponent>.CreateStore();
        Transforms = GenericStores<Transform>.CreateStore();
        BoundingBoxes = GenericStores<BoxComponent>.CreateStore();
        Animations = GenericStores<AnimationComponent>.CreateStore();
    }

    public int EntityCount => _idIdx;


    internal void AttachRender(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }


    public EntityId CreateModelEntity(ModelId model, int drawCount, MaterialTag materialTag, in Transform transform,
        in BoundingBox boundingBox)
    {
        var entityId = Create();
        var matKey = _materialTable.Add(materialTag);
        Models.Add(entityId, new ModelComponent(model, drawCount, matKey));
        Transforms.Add(entityId, in transform);
        BoundingBoxes.Add(entityId, new BoxComponent(in boundingBox));
        return entityId;
    }


    internal void EndTick()
    {
        Transforms.EndTick();
        Models.EndTick();

        //Transforms2D.EndTick();
        //Sprites.EndTick();
    }


    public EntityEnumerator<T1> Query<T1>() where T1 : unmanaged => new(GenericStores<T1>.Store);

    public EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store);

    public EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged =>
        new(GenericStores<T1>.Store, GenericStores<T2>.Store, GenericStores<T3>.Store);


    private static class GenericStores<T> where T : unmanaged
    {
        public static EntityStore<T> Store { get; private set; } = null!;

        public static EntityStore<T> CreateStore()
        {
            var store = new EntityStore<T>();
            Store = store;
            return store;
        }
    }
}