using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;

namespace ConcreteEngine.Engine.Worlds;

public sealed class World : GameEngineSystem
{
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

    internal readonly WorldBundle Bundle;

    internal World(EngineWindow window, AssetSystem assets)
    {
        _assets = assets;
        _camera = new Camera(window.OutputSize);
        _meshGenerator = new MeshGeneratorRegistry();

        _meshTable = new MeshTable();
        _materialTable = new MaterialTable();
        _animationTable = new AnimationTable();

        _sky = new WorldSky();
        _terrain = new Terrain(_meshTable, _materialTable);
        _particles = new ParticleSystem(_meshTable, _materialTable);

        _worldVisual = new WorldVisual(window.OutputSize);

        _rayCast = new RayCaster(Camera, _terrain);
        Bundle = MakeBundle();

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


    internal void Initialize(AssetSystem assets, FrameEntityBuffer frameBuffer, GfxContext gfx)
    {
        _rayCast.FrameBuffer = frameBuffer;
        
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
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };
        
        DrawTagResolver.BoundsMaterial = mat.Id;
    }

    internal void Update(float dt, Size2D viewport)
    {
        Camera.StartTick(viewport);
    }

    internal void EndUpdate(RenderCamera renderCamera)
    {
        WorldVisual.EndTick();
        Camera.EndTick(WorldVisual, ref renderCamera.LightSpace);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(fixedDt);
    }

    private WorldBundle MakeBundle() =>
        new()
        {
            AnimationTable = _animationTable,
            MeshTable = _meshTable,
            MaterialTable = _materialTable,
            Camera = _camera,
            ParticleSystem = _particles,
            Terrain = _terrain,
            Sky = _sky
        };
}