using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor.Data;
using ConcreteEngine.Engine.Editor.Definitions;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Game;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Configuration;
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

    private readonly WorldSky _sky;
    private readonly Terrain _terrain;
    private readonly ParticleSystem _particles;

    private readonly WorldVisual _worldVisual;

    private readonly RayCaster _rayCast;
    private readonly Camera _camera;

    private readonly MeshTable _meshTable;
    private readonly MaterialTable _materialTable;
    private readonly AnimationTable _animationTable;
    private readonly MeshGeneratorRegistry _meshGenerator;


    private readonly RenderWorld _renderWorld;

    private readonly GameSystem _gameSystem;

    private bool _hasUploadedMaterial = false;

    private RenderCamera RenderCamera => _renderEngine.RenderCamera;

    internal World(EngineWindow window, GraphicsRuntime graphics, RenderEngine renderEngine, AssetSystem assets)
    {
        _gfxCommands = graphics.Gfx.Commands;
        _window = window;
        _renderEngine = renderEngine;
        _assets = assets;
        _camera = new Camera();
        _meshGenerator = new MeshGeneratorRegistry();

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _sky = new WorldSky();
        _terrain = new Terrain(_meshTable, _materialTable);
        _particles = new ParticleSystem(_meshTable, _materialTable);

        _gameSystem = new GameSystem();

        _worldVisual = new WorldVisual(AssetConfigLoader.GraphicSettings);

        _renderWorld = new RenderWorld(new RenderContext
        {
            AnimationTable = _animationTable,
            MeshTable = _meshTable,
            MaterialTable = _materialTable,
            Camera = _camera,
            ParticleSystem = _particles
        });
        _rayCast = new RayCaster(Camera, _terrain, _renderWorld.DrawEntityPipeline);

        _renderEngine.SetRenderParams(_worldVisual.Snapshot);
        
        Ecs.InitGameEcs();
        Ecs.InitRenderEcs();
    }


    public Camera Camera => _camera;
    public RayCaster RayCast => _rayCast;

    public WorldSky Sky => _sky;
    public Terrain Terrain => _terrain;
    public ParticleSystem Particles => _particles;

    public WorldVisual WorldVisual => _worldVisual;

    internal MeshTable MeshTableImpl => _meshTable;
    internal MaterialTable MaterialTableImpl => _materialTable;
    internal AnimationTable AnimationTableImpl => _animationTable;

    public int EntityCount => Ecs.Render.Core.Count;

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
        DrawEntityPipeline.BoundsMaterial = mat.Id;
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
        _renderWorld.BeforeRender();

        _camera.WriteSnapshot(EngineTime.GameAlpha, RenderCamera);

        _renderEngine.PrepareFrame(in frameInfo, in runtimeParams);

        // Upload materials
        SubmitMaterialData();

        // Upload draw commands
        _renderWorld.Execute(this, _renderEngine.CommandBuffer);

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
        
        WorldVisual.EndTick();
        Camera.EndTick(WorldVisual.Snapshot, RenderCamera);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(fixedDt);
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
                if (_worldVisual.SetShadow(req.Size.Width))
                    _renderEngine.FboRegistry.RecreateFixedFrameBuffer<ShadowPassTag>(FboVariant.Default, req.Size);
                break;
            case FboCommandAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(req.Action));
        }
    }

    public void Shutdown() {}
}