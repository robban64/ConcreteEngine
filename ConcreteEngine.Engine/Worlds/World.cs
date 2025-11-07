#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
using ConcreteEngine.Engine.Worlds.View;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public interface IWorld
{
    public int EntityCount { get; }

    public Camera3D Camera { get; }

    WorldRenderParams WorldRenderParams { get; }
    WorldSkybox Sky { get; }
    WorldTerrain Terrain { get; }

    IMeshTable MeshTable { get; }
    IMaterialTable EntityMaterials { get; }


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

    private WorldSkybox _sky = null!;
    private WorldTerrain _terrain = null!;

    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;

    public Camera3D Camera { get; }
    public WorldRenderParams WorldRenderParams { get; }

    internal World()
    {
        Camera = new Camera3D();
        WorldRenderParams = new WorldRenderParams();

        Transforms2D = GenericStores<Transform2D>.CreateStore();
        Transforms = GenericStores<Transform>.CreateStore();
        Meshes = GenericStores<ModelComponent>.CreateStore();
        Sprites = GenericStores<SpriteComponent>.CreateStore();
    }

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;

    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;

    public int EntityCount => _idIdx;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;

    internal void AttachRender(BatcherRegistry batchers, MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;

        _terrain = new WorldTerrain(batchers.Get<TerrainBatcher>());
        _sky = new WorldSkybox();

        Terrain.AttachModelRegistry(meshTable);
        Sky.AttachModelRegistry(meshTable);
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
        if (cmd is EntityCommandRecord<TransformEditorModel> transformCmd)
        {
            ref readonly var transform = ref transformCmd.Data;
            ref var entityTransform = ref Transforms.GetById(new EntityId(transformCmd.EntityId));
            entityTransform.Translation = transform.Translation;
            entityTransform.Scale = transform.Scale;
            entityTransform.Rotation = transform.Rotation;
        }
        else if (cmd is CameraCommandRecord cameraCmd)
        {
            ref readonly var data = ref cameraCmd.Data;
            Camera.Translation = data.ViewTransform.Translation;
            Camera.Scale = data.ViewTransform.Scale;
            Camera.Orientation = data.ViewTransform.Orientation;
            Camera.FarPlane = data.Projection.Far;
            Camera.NearPlane = data.Projection.Near;
            Camera.Fov = data.Projection.Fov;
        }
        else
        {
            throw new InvalidOperationException("Unknown Command");
        }
    }

    internal void UpdateTick(Size2D viewSize)
    {
        if (Camera.Viewport != viewSize) Camera.Viewport = viewSize;
    }

    internal void EndTick()
    {
        Camera.EndTick();
        Cleanup();
    }

    private void Cleanup()
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