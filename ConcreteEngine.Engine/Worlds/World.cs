#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Shared.RenderData;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public interface IWorld
{
    int EntityCount { get; }

    Camera3D Camera { get; }
    WorldEntities Entities { get; }

    WorldRenderParams WorldRenderParams { get; }
    WorldSkybox Sky { get; }
    WorldTerrain Terrain { get; }

    IMeshTable MeshTable { get; }
    IMaterialTable EntityMaterials { get; }
}

public sealed class World : IWorld
{
    public Camera3D Camera { get; }
    public WorldRenderParams WorldRenderParams { get; }

    public WorldEntities Entities { get; }

    private WorldSkybox _sky = null!;
    private WorldTerrain _terrain = null!;

    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;


    internal World()
    {
        Camera = new Camera3D();
        WorldRenderParams = new WorldRenderParams();
        Entities = new WorldEntities();
    }

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;

    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;

    public int EntityCount => Entities.EntityCount;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;

    internal void AttachRender(BatcherRegistry batchers, MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;

        _terrain = new WorldTerrain(batchers.Get<TerrainBatcher>());
        _sky = new WorldSkybox();

        Entities.AttachRender(meshTable, materialTable);
        Terrain.AttachModelRegistry(meshTable);
        Sky.AttachModelRegistry(meshTable);
    }

    internal void ProcessActions()
    {
        var entities = Entities;

        if (WorldActionSlot.SelectedEntityId > 0)
        {
            //var model = entities.Meshes.GetById(WorldActionSlot.SelectedEntityId);
        }

        if (!WorldActionSlot.IsDirty) return;
        if (WorldActionSlot.TryReadSlot(WorldRenderParams.Version, out WorldParamsData worldData))
            WorldRenderParams.FromEditor(in worldData);

        if (WorldActionSlot.TryReadSlot(Camera.Generation, out CameraEditorPayload cameraData))
        {
            ref readonly var data = ref cameraData;
            Camera.Translation = data.ViewTransform.Translation;
            Camera.Scale = data.ViewTransform.Scale;
            Camera.Orientation = data.ViewTransform.Orientation;
            Camera.FarPlane = data.Projection.Far;
            Camera.NearPlane = data.Projection.Near;
            Camera.Fov = data.Projection.Fov;
        }

        if (WorldActionSlot.TryReadSlot(0, out EntityDataPayload entityData))
        {
            ref readonly var transform = ref entityData.Transform;
            ref var entityTransform = ref entities.Transforms.GetById(new EntityId(entityData.EntityId));
            entityTransform.Translation = transform.Translation;
            entityTransform.Scale = transform.Scale;
            entityTransform.Rotation = transform.Rotation;
        }

        WorldActionSlot.ClearDirty();
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
        if (cmd is EntityCommandRecord<TransformData> transformCmd)
        {
        }
        else if (cmd is CameraCommandRecord cameraCmd)
        {
        }
        else
        {
            throw new InvalidOperationException("Unknown Command");
        }
    }

    internal void StartTick(Size2D viewSize)
    {
        Camera.Viewport = viewSize;
    }

    internal void EndTick()
    {
        Entities.EndTick();
        Camera.EndTick();
    }
}