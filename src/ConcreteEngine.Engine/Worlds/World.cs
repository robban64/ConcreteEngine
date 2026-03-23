namespace ConcreteEngine.Engine.Worlds;
/*
public sealed class World : GameEngineSystem
{

    internal readonly WorldBundle Bundle;


    internal World()
    {

        Bundle = MakeBundle();
    }

    public Skybox Sky => _sky;
    public Terrain Terrain => _terrain;
    public ParticleSystem Particles => _particles;

    internal void Initialize( AssetSystem assets, GfxContext gfx)
    {
        Animations.Setup(assets);
        MeshGenerator.Register(new TerrainMeshGenerator(gfx, _terrain));
        _particles.AttachRenderer(MeshGenerator.Register(new ParticleMeshGenerator(gfx)));

        RenderDispatcher.TerrainMesh = MeshGenerator.Get<TerrainMeshGenerator>();

        var mat = assets.MaterialStore.CreateMaterial("EmptyMat", "EmptyMat1");
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };


        DrawTagResolver.BoundsMaterial = mat.MaterialId;
    }
    internal void OnSimulationTick(float fixedDt)
    {
        _particles.UpdateSimulate(fixedDt);
    }

    private WorldBundle MakeBundle() => new()
    {
        Animations = Animations, ParticleSystem = _particles, Terrain = _terrain, Sky = _sky, MeshRegistry = MeshGenerator
    };
}*/