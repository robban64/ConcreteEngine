#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Shared.Rendering;

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

    private readonly MeshGeneratorRegistry _meshGenerators;

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

        _meshGenerators = new MeshGeneratorRegistry();

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

    internal MeshTable GetMeshTableImpl() => _meshTable;

    internal MaterialTable GetMaterialTableImpl() => _materialTable;

    public int EntityCount => Entities.EntityCount;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;


    internal void AttachRender(GfxContext gfx, MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;

        Entities.AttachRender(_meshTable, _materialTable);
        Sky.AttachRenderer(_meshTable);
        Terrain.AttachRenderer(_meshGenerators.Register(new TerrainMeshGenerator(gfx)), _meshTable, _materialTable);
        _particles.AttachRenderer(_meshGenerators.Register(new ParticleMeshGenerator(gfx)), _materialTable);
    }

    internal void StartUpdate(Size2D viewSize, float dt)
    {
        Camera.Viewport = viewSize;
    }

    internal void StartTick(float fixedDt, float totalTime)
    {
        ProcessActions();
    }

    internal void EndTick()
    {
        Entities.EndTick();
        Camera.EndTick();
    }

    internal void OnSimulationTick(UpdateTickerArgs args)
    {
        _particles.UpdateSimulate(_entities, args.FixedDt, args.Alpha);
    }

    internal void OnPreRender(float alpha)
    {
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
    }

    private void ProcessActions()
    {
        var entities = Entities;

        if (WorldActionSlot.SelectedEntityId > 0)
        {
            //var model = entities.Meshes.GetById(WorldActionSlot.SelectedEntityId);
        }

        if (!WorldActionSlot.IsDirty) return;

        if (WorldActionSlot.HasPendingSlot<WorldParamsData>(WorldRenderParams.Version))
            WorldRenderParams.FromEditor(in WorldActionSlot.ReadSlot<WorldParamsData>());


        if (WorldActionSlot.HasPendingSlot<CameraDataState>(Camera.Generation))
        {
            Camera.FromEditor(in WorldActionSlot.ReadSlot<CameraDataState>());
        }

        if (WorldActionSlot.HasPendingSlot<EntityDataState>(0))
        {
            Console.WriteLine("Entity transform updated");
            ref readonly var entityData = ref WorldActionSlot.ReadSlot<EntityDataState>();
            var view = entities.Core.GetEntityView(new EntityId(entityData.EntityId));
            view.Transform = entityData.GetTransform();
            view.Box.Bounds = entityData.Bounds;
        }

        WorldActionSlot.ClearDirty();
    }
}