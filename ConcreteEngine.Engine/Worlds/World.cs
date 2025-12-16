using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Worlds;

public sealed class World
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


        _meshGenerator = new MeshGeneratorRegistry();

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _entities = new WorldEntities();
        _sky = new WorldSkybox();
        _terrain = new WorldTerrain(_meshTable, _materialTable);
        _particles = new WorldParticles(_meshTable, _materialTable);

        _drawEntities = new DrawEntityAssembler(this);

        _raycast = new WorldRaycaster(Camera, Entities, _terrain, _drawEntities);

        _worldRenderParams = new WorldRenderParams(AssetConfigLoader.GraphicSettings);
        _worldRenderParams.EndTick();

        _worldRenderer = new WorldRenderer(engineWindow, graphics, assets, _worldRenderParams, _drawEntities, _camera);
    }

    internal WorldEntities Entities => _entities;

    internal WorldRenderer Renderer => _worldRenderer;

    public Camera3D Camera => _camera;
    public WorldRaycaster Raycast => _raycast;

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;
    public WorldParticles Particles => _particles;

    public WorldRenderParams WorldRenderParams => _worldRenderParams;

    public IMeshTable MeshTable => _meshTable;
    public IMaterialTable EntityMaterials => _materialTable;

    internal MeshTable MeshTableImpl => _meshTable;
    internal MaterialTable MaterialTableImpl => _materialTable;
    internal AnimationTable AnimationTableImpl => _animationTable;

    internal MeshGeneratorRegistry MeshGenerator => _meshGenerator;

    public int EntityCount => Entities.EntityCount;
    public int ShadowMapSize => WorldRenderParams.Snapshot.Shadows.ShadowMapSize;

    internal void Initialize(AssetSystem assets, GfxContext gfx)
    {
        _meshTable.Setup(_assets);
        _animationTable.Setup(_assets);

        Terrain.AttachRenderer(_meshGenerator.Register(new TerrainMeshGenerator(gfx)));
        _particles.AttachRenderer(_meshGenerator.Register(new ParticleMeshGenerator(gfx)));
        _entities.Attach(_meshTable, _materialTable);
        _sky.AttachRenderer(_meshTable);


        PrimitiveMeshes.Cube = _assets.StoreImpl.GetByName<Model>("Cube").MeshParts[0].ResourceId;
        var mat = assets.MaterialStoreImpl.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.State.Pipeline = new MaterialPipelineState
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassStateFunc(BlendMode.Alpha)
        };
        _drawEntities.BoundsMaterial = mat.Id;
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

    internal WorldContext CreateContext() =>
        new()
        {
            Entities = _entities,
            MeshTable = _meshTable,
            MaterialTable = _materialTable,
            AnimationTable = _animationTable,
            WorldRenderParams = _worldRenderParams,
            MeshGenerator = _meshGenerator,
            Camera = _camera,
            Particles = _particles,
            Sky = _sky,
            Raycast = _raycast,
            Terrain = _terrain,
        };
}