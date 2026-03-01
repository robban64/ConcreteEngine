using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
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

    private readonly MeshGeneratorRegistry _meshGenerator;

    internal readonly WorldBundle Bundle;

    internal AnimationTable Animations { get; }

    internal World(EngineWindow window, AssetSystem assets, RenderParamsSnapshot snapshot)
    {
        _assets = assets;
        _worldVisual = new WorldVisual(snapshot, window.OutputSize);
        _camera = new Camera(window.OutputSize);
        _meshGenerator = new MeshGeneratorRegistry();

        _sky = new WorldSky();
        _terrain = new Terrain();
        _particles = new ParticleSystem();

        _rayCast = new RayCaster(Camera, _terrain);

        Animations = new AnimationTable();
        Bundle = MakeBundle();
    }


    public Camera Camera => _camera;
    public RayCaster RayCast => _rayCast;

    public WorldSky Sky => _sky;
    public Terrain Terrain => _terrain;
    public ParticleSystem Particles => _particles;

    public WorldVisual WorldVisual => _worldVisual;


    internal void Initialize(AssetSystem assets, FrameEntityBuffer frameBuffer, GfxContext gfx)
    {
        _rayCast.FrameBuffer = frameBuffer;
        Animations.Setup(assets);

        Terrain.AttachRenderer(_meshGenerator.Register(new TerrainMeshGenerator(gfx)));
        _particles.AttachRenderer(_meshGenerator.Register(new ParticleMeshGenerator(gfx)));

        PrimitiveMeshes.Cube = _assets.Store.GetByName<Model>("Cube").Meshes[0].MeshId;
        var mat = assets.MaterialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };


        DrawTagResolver.BoundsMaterial = mat.MaterialId;
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

    private WorldBundle MakeBundle() => new()
    {
        Animations = Animations,
        Camera = _camera,
        ParticleSystem = _particles,
        Terrain = _terrain,
        Sky = _sky
    };
}