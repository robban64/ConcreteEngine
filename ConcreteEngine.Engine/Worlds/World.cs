using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Game;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Worlds;

public sealed class World : IGameEngineSystem
{
    private readonly GfxCommands _gfxCommands;
    private readonly EngineWindow _window;
    private readonly RenderEngine _renderEngine;
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


    private readonly DrawEntityPipeline _drawEntities;
    private readonly WorldRenderer _worldRenderer;

    private readonly EntityWorld _ecs;
    private readonly GameSystem _gameSystem;

    private bool _hasUploadedMaterial = false;

    private RenderCamera RenderCamera => _renderEngine.RenderCamera;

    internal World(EngineWindow window, GraphicsRuntime graphics, RenderEngine renderEngine, AssetSystem assets, EntityWorld ecs)
    {
        _gfxCommands = graphics.Gfx.Commands;
        _window = window;
        _renderEngine = renderEngine;
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

        _drawEntities = new DrawEntityPipeline(this);
        _gameSystem = new GameSystem(ecs.GameEntity);

        _raycast = new WorldRaycaster(Camera, Entities, _terrain, _drawEntities);

        _worldRenderParams = new WorldRenderParams(AssetConfigLoader.GraphicSettings);
        _worldRenderParams.EndTick();

        _worldRenderer = new WorldRenderer(_ecs.GameEntity, _ecs.RenderEntity, _drawEntities, _camera);
        
        _renderEngine.SetRenderParams(_worldRenderParams.Snapshot);
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

    public int EntityCount => Entities.EntityCount;

    internal void Initialize(AssetSystem assets, GfxContext gfx)
    {
        _meshTable.Setup(_assets);
        _animationTable.Setup(_assets);

        Terrain.AttachRenderer(_meshGenerator.Register(new TerrainMeshGenerator(gfx)));
        _particles.AttachRenderer(_meshGenerator.Register(new ParticleMeshGenerator(gfx)));
        _sky.AttachRenderer(_meshTable);


        PrimitiveMeshes.Cube = _assets.Store.GetByName<Model>("Cube").MeshParts[0].ResourceId;
        var mat = assets.MaterialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.State.Pipeline = new MaterialPipelineState
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassStateFunc(BlendMode.Alpha)
        };
        _drawEntities.BoundsMaterial = mat.Id;
    }
    
    private void SubmitMaterialData()
    {
        var matStore = _assets.MaterialStore;
        if(!matStore.HasDirtyMaterials && _hasUploadedMaterial) return;
        if (matStore.HasDirtyMaterials) _hasUploadedMaterial = false;
       
        matStore.ClearDirtyMaterials();
        foreach (var material in matStore.MaterialSpan)
        {
            matStore.GetMaterialUploadData(material!, out var payload);
            _renderEngine.SubmitMaterialDrawData(in payload, material!.TextureSlots.CacheSlots);
        }

        _hasUploadedMaterial = true;
    }

    internal void PreRender(
        BeginFrameStatus status,
        RenderFrameInfo frameInfo,
        RenderRuntimeParams runtimeParams)
    {
        _worldRenderer.BeforeRender();

        _drawEntities.Reset();

        _camera.WriteSnapshot(EngineTime.GameAlpha, RenderCamera);

        _renderEngine.PrepareFrame(in frameInfo, in runtimeParams);

        // Upload materials
        SubmitMaterialData();

        // Upload draw commands
        _drawEntities.Execute(_renderEngine.CommandBuffer);

        // fill buffers
        _renderEngine.CollectDrawBuffers();

        _renderEngine.StartFrame(status);
    }

    internal void ExecuteFrame(out GfxFrameResult frameResult)
    {
        _renderEngine.UploadFrameData();
        _renderEngine.Render();
        _renderEngine.EndRenderFrame(out frameResult);
    }


    
    
    internal void UpdateTick(float dt, Size2D viewport)
    {
        Camera.StartTick(viewport);
    }

    internal void EndUpdateTick(float dt)
    {
        //Entities.EndTick();
        
        _gameSystem.UpdateTick(dt);
        
        WorldRenderParams.EndTick();
        Camera.EndTick(WorldRenderParams.Snapshot, RenderCamera);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(_ecs.RenderEntity, fixedDt);
    }

    internal void ProcessCommand(IWorldCommandRecord cmd)
    {
    }
    
    internal void RecreateFrameBuffer(FboCommandRecord req)
    {
        _gfxCommands.BindFramebuffer(default);
        _gfxCommands.UnbindAllTextures();

        switch (req.Action)
        {
            case FboCommandAction.RecreateScreenDependentFbo:
                _renderEngine.FboRegistry.RecreateScreenDependentFbo(_window.OutputSize);
                break;
            case FboCommandAction.RecreateShadowFbo:
                if (_worldRenderParams.SetShadow(req.Size.Width))
                    _renderEngine.FboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default, req.Size);
                break;
            case FboCommandAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(req.Action));
        }
    }

    public void Shutdown() {}
}