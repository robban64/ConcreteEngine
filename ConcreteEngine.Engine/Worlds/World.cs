#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics.Gfx;
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
    WorldParticles Particles { get; }

    WorldRaycaster Raycast { get; }


    IMeshTable MeshTable { get; }
    IMaterialTable EntityMaterials { get; }
}

public sealed class World : IWorld
{
    public Camera3D Camera { get; }
    public WorldRenderParams WorldRenderParams { get; }

    public WorldRaycaster Raycast { get; }

    private readonly BatcherRegistry _batchers;

    private readonly WorldEntities _entities;
    private readonly WorldSkybox _sky;
    private readonly WorldTerrain _terrain;
    private readonly WorldParticles _particles;

    private MeshTable _meshTable = null!;
    private MaterialTable _materialTable = null!;


    internal World()
    {
        Camera = new Camera3D();
        WorldRenderParams = new WorldRenderParams();

        _batchers = new BatcherRegistry();

        _entities = new WorldEntities();
        _sky = new WorldSkybox();
        _terrain = new WorldTerrain();
        _particles = new WorldParticles();

        Raycast = new WorldRaycaster(Camera, Entities, _terrain);
    }

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;
    public WorldEntities Entities => _entities;
    public WorldParticles Particles => _particles;


    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;

    public int EntityCount => Entities.EntityCount;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;


    internal void AttachRender(GfxContext gfx, MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;

        Entities.AttachRender(_meshTable, _materialTable);
        Sky.AttachRenderer(_meshTable);
        Terrain.AttachRenderer(_batchers.Register(new TerrainBatcher(gfx)), _meshTable, _materialTable);
        _particles.AttachRenderer(_batchers.Register(new ParticleBatcher(gfx)), _meshTable, _materialTable);
    }

    internal void StartUpdate(Size2D viewSize, float dt)
    {
        Camera.Viewport = viewSize;
    }

    internal void StartTick(float fixedDt, float totalTime)
    {
        ProcessActions();
        _particles.Simulate(fixedDt, totalTime, Camera.Translation);
    }

    internal void EndTick()
    {
        Entities.EndTick();
        Camera.EndTick();
    }

    internal void OnPreRender(float alpha)
    {
        _particles?.ProcessAndUpload(alpha);
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

    private void ProcessActions()
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
            ref var entityTransform = ref entities.Core.GetTransformById(new EntityId(entityData.EntityId));
            entityTransform.Translation = transform.Translation;
            entityTransform.Scale = transform.Scale;
            entityTransform.Rotation = transform.Rotation;
        }

        WorldActionSlot.ClearDirty();
    }
}