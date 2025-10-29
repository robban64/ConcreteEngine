#region

using ConcreteEngine.Core.World.Entities;
using ConcreteEngine.Core.World.Render;
using ConcreteEngine.Core.World.Render.Batching;

#endregion

namespace ConcreteEngine.Core.World;

public interface IWorld
{
    public int EntityCount { get; }

    WorldRenderParams WorldRenderParams { get; }
    WorldSkybox Sky { get; }
    WorldTerrain Terrain { get; }


    EntityId Create();
    EntityStore<Transform> Transforms { get; }
    EntityStore<ModelComponent> Meshes { get; }
    EntityStore<Transform2D> Transforms2D { get; }
    EntityStore<SpriteComponent> Sprites { get; }

    EntityEnumerator<T1> Query<T1>() where T1 : unmanaged;
    EntityEnumerator<T1, T2> Query<T1, T2>() where T1 : unmanaged where T2 : unmanaged;
    EntityEnumerator<T1, T2, T3> Query<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
}

public sealed class World : IWorld
{
    public EntityId Create() => new(_idIdx++);
    private int _idIdx = 1;

    public WorldRenderParams WorldRenderParams { get; }
    public WorldSkybox Sky { get; }
    public WorldTerrain Terrain { get; }

    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    internal World(WorldRenderParams worldRenderParams, BatcherRegistry batchers)
    {
        WorldRenderParams = worldRenderParams;
        Terrain = new WorldTerrain(batchers.Get<TerrainBatcher>());
        Sky = new WorldSkybox();

        Transforms2D = GenericStores<Transform2D>.CreateStore();
        Transforms = GenericStores<Transform>.CreateStore();
        Meshes = GenericStores<ModelComponent>.CreateStore();
        Sprites = GenericStores<SpriteComponent>.CreateStore();
    }

    public int EntityCount => _idIdx;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;

    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;


    internal void AttachRender(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
        Terrain.AttachModelRegistry(meshTable);
        Sky.AttachModelRegistry(meshTable);
    }


    public void Cleanup()
    {
        Transforms.Cleanup();
        Transforms2D.Cleanup();
        Sprites.Cleanup();
        Meshes.Cleanup();
    }

    public EntityStore<Transform> Transforms { get; }
    public EntityStore<ModelComponent> Meshes { get; }
    public EntityStore<Transform2D> Transforms2D { get; }
    public EntityStore<SpriteComponent> Sprites { get; }


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