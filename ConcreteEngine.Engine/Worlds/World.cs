using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Worlds;

public sealed class World
{
    private readonly AssetSystem _assets;

    private readonly WorldSkybox _sky;
    private readonly WorldTerrain _terrain;
    private readonly WorldParticles _particles;

    private readonly WorldRenderParams _worldRenderParams;

    private readonly WorldRaycaster _raycast;
    private readonly Camera3D _camera;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;
    private readonly AnimationTable _animationTable;
    private readonly MeshGeneratorRegistry _meshGenerator;


    private readonly DrawEntityAssembler _drawEntities;
    private readonly WorldRenderer _worldRenderer;

    private readonly EntityWorld _ecs;


    internal World(EngineWindow engineWindow, GraphicsRuntime graphics, AssetSystem assets, EntityWorld ecs)
    {
        _assets = assets;
        _ecs = ecs;
        _camera = new Camera3D();
        _meshGenerator = new MeshGeneratorRegistry();

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _sky = new WorldSkybox();
        _terrain = new WorldTerrain(_meshTable, _materialTable);
        _particles = new WorldParticles(_meshTable, _materialTable);

        _drawEntities = new DrawEntityAssembler(this);

        _raycast = new WorldRaycaster(Camera, Entities, _terrain, _drawEntities);

        _worldRenderParams = new WorldRenderParams(AssetConfigLoader.GraphicSettings);
        _worldRenderParams.EndTick();

        _worldRenderer = new WorldRenderer(engineWindow, graphics, assets, _worldRenderParams, _drawEntities, _camera);
    }

    internal RenderEntityHub Entities => _ecs.RenderEntity;

    internal WorldRenderer Renderer => _worldRenderer;

    public Camera3D Camera => _camera;
    public WorldRaycaster Raycast => _raycast;

    public WorldSkybox Sky => _sky;
    public WorldTerrain Terrain => _terrain;
    public WorldParticles Particles => _particles;

    public WorldRenderParams WorldRenderParams => _worldRenderParams;

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

    internal void StartTick(Size2D viewport)
    {
        Camera.StartTick(viewport);
    }

    internal void EndTick()
    {
        Entities.EndTick();
        WorldRenderParams.EndTick();
        Camera.EndTick(WorldRenderParams.Snapshot, _worldRenderer.RenderCamera);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(_ecs.RenderEntity, fixedDt);
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
    }


    internal WorldContext CreateContext() =>
        new(_ecs.RenderEntity, _sky, _terrain, _particles, _meshTable, _materialTable, _animationTable);
}