using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

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
    private readonly AssetSystem _assets;

    private readonly WorldEntities _entities;
    private readonly WorldSkybox _sky;
    private readonly WorldTerrain _terrain;
    private readonly WorldParticles _particles;
    private readonly WorldRaycaster _raycast;
    private readonly WorldRenderParams _worldRenderParams;

    private readonly MeshGeneratorRegistry _meshGenerator;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;
    private readonly AnimationTable _animationTable;

    private readonly Camera3D _camera;

    private readonly DrawEntityAssembler _drawEntities;
    private readonly WorldRenderer _worldRenderer;

    internal World(EngineWindow engineWindow, GraphicsRuntime graphics, AssetSystem assets)
    {
        _assets = assets;
        _camera = new Camera3D();

        _worldRenderParams = new WorldRenderParams();
        _worldRenderParams.EndTick();

        _meshGenerator = new MeshGeneratorRegistry();

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _drawEntities = new DrawEntityAssembler();

        _entities = new WorldEntities();
        _sky = new WorldSkybox();
        _terrain = new WorldTerrain();
        _particles = new WorldParticles();

        _raycast = new WorldRaycaster(Camera, Entities, _terrain, _drawEntities);

        _worldRenderer = new WorldRenderer(engineWindow, graphics, assets, _worldRenderParams, _drawEntities, _camera);
    }

    internal WorldRenderer Renderer => _worldRenderer;

    public Camera3D Camera => _camera;
    public WorldRaycaster Raycast => _raycast;

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;
    public WorldEntities Entities => _entities;
    public WorldParticles Particles => _particles;

    public WorldRenderParams WorldRenderParams => _worldRenderParams;

    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;

    internal MeshTable GetMeshTableImpl() => _meshTable;
    internal MaterialTable GetMaterialTableImpl() => _materialTable;

    internal AnimationTable GetAnimationTableImpl() => _animationTable;


    public int EntityCount => Entities.EntityCount;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;

    internal void Initialize(AssetSystem assets, GfxContext gfx)
    {
        _meshTable.Setup(_assets);
        _animationTable.Setup(_assets);
        _drawEntities.AttachWorld(this);

        Terrain.AttachRenderer(_meshGenerator.Register(new TerrainMeshGenerator(gfx)), _meshTable, _materialTable);
        _particles.AttachRenderer(_meshGenerator.Register(new ParticleMeshGenerator(gfx)), _materialTable);
        _entities.Attach(_meshTable, _materialTable);
        _sky.AttachRenderer(_meshTable);


        _drawEntities.CubeId = _assets.StoreImpl.GetByName<Model>("Cube").ModelId;
        var mat = assets.MaterialStoreImpl.CreateMaterial("EmptyMat", "EmptyMat1");
        _drawEntities.EmptyMaterialKey = _materialTable.Add(MaterialTagBuilder.BuildOne(mat.Id, true));

        DrawDataProvider.Attach(_worldRenderer.RenderEngine.CommandBuffer, _animationTable, _meshTable, _materialTable,
            _entities);
    }

    internal void StartTick(Size2D viewSize)
    {
        Camera.Viewport = viewSize;
        ProcessActions();
        Camera.StartTick();
    }

    internal void EndTick()
    {
        Entities.EndTick();
        WorldRenderParams.EndTick();
        Camera.EndTick(WorldRenderParams.Snapshot, _worldRenderer.RenderCamera);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(_entities, fixedDt);
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
    }

    private void ProcessActions()
    {
    }
}