using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Worlds;

public sealed class World : GameEngineSystem
{
    private readonly AssetSystem _assets;

    private readonly Skybox _sky;
    private readonly Terrain _terrain;
    private readonly ParticleSystem _particles;

    private readonly MeshGeneratorRegistry _meshGenerator;

    internal readonly WorldBundle Bundle;

    internal AnimationTable Animations { get; }

    internal World(EngineWindow window, AssetSystem assets)
    {
        _assets = assets;

        _meshGenerator = new MeshGeneratorRegistry();

        _sky = new Skybox();
        _terrain = new Terrain();
        _particles = new ParticleSystem();

        Animations = new AnimationTable();
        Bundle = MakeBundle();
    }

    public Skybox Sky => _sky;
    public Terrain Terrain => _terrain;
    public ParticleSystem Particles => _particles;

    internal void Initialize(SceneManager sceneManager, AssetSystem assets, GfxContext gfx)
    {
        CameraSystem.Instance.AttachRaycast(sceneManager,Terrain);

        Animations.Setup(assets);

        Terrain.AttachRenderer(_meshGenerator.Register(new TerrainMeshGenerator(gfx)));
        _particles.AttachRenderer(_meshGenerator.Register(new ParticleMeshGenerator(gfx)));

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
        CameraSystem.Instance.Camera.BeginUpdate(viewport);
    }

    internal void EndUpdate()
    {
        var visualEnv = VisualSystem.Instance.VisualEnv;
        visualEnv.Ensure();

        var lightDir = visualEnv.GetDirectionalLight().Direction;
        CameraSystem.Instance.Camera.EndUpdate(in visualEnv.GetShadow(), lightDir);
    }

    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(fixedDt);
    }

    private WorldBundle MakeBundle() => new()
    {
        Animations = Animations, ParticleSystem = _particles, Terrain = _terrain, Sky = _sky
    };
}