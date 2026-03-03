using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Descriptors;

namespace Demo3D;

public sealed class Demo3DScene : GameScene
{
    private EntitySpawner _spawner = null!;

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<FlyCameraModule>(0);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        builder.RegisterRender(new RenderTargetDescriptor());
    }


    public override void Update(float deltaTime) { }

    public override void UpdateTick(float deltaTime) { }

    public override void Initialize()
    {
        var assets = Context.GetSystem<AssetSystem>();

        CreateTerrain(assets);

        CreateSky(assets);

        CreateParticles(assets);

        CreateKnight(assets);
        CreateWarrior(assets);
        //CreateCesiumMan(assets);
        CreateSpawner(assets);

        //CreateWell(assets);
        //CreateForestHut(assets);
        //CreateGallows(assets);
        //CreateTowerBridge(assets);
        //CreateWagon(assets);
        _spawner = null!;

        float half = 256 / 2f;
        var worldTerrain = Context.World.Terrain;
        Camera.Translation = new Vector3(half - 30, worldTerrain.GetSmoothHeight(half - 30, half + 30) + 4f, half + 30);
    }

    public override void Unload()
    {
    }


    private void CreateTerrain(AssetSystem assets)
    {
        var heightmap = assets.Store.GetByName<Texture>("Heightmap");
        var terrainMat = assets.MaterialStore.Get("TerrainMat");
        terrainMat.UvRepeat = 14;
        terrainMat.Shininess = 4;
        terrainMat.Specular = 0.02f;

        var worldTerrain = Context.World.Terrain;
        worldTerrain.CreateTerrainMesh(heightmap);
        worldTerrain.SetMaterial(terrainMat.MaterialId);
    }

    private void CreateSky(AssetSystem assets)
    {
        var skyboxMaterial = assets.MaterialStore.Get("SkyboxMat");
        skyboxMaterial.Pipeline = new MaterialPipeline(
            GfxPassState.Disable(GfxStateFlags.DepthWrite),
            GfxPassFunctions.MakeSky());

        Context.World.Sky.SetSkyMaterial(skyboxMaterial.MaterialId);
    }

    private void CreateParticles(AssetSystem assets)
    {
        var sceneManager = Context.SceneManager;

        var particleMat = assets.MaterialStore.Get("ParticleMat");
        particleMat.Transparency = true;
        particleMat.Color = new Color4(0.55f, 0.85f, 0.45f);
        particleMat.Shininess = 0f;
        particleMat.Specular = 0f;

        particleMat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };

        var worldParticles = Context.World.Particles;
        worldParticles.SetMaterial(particleMat.MaterialId);

        var def = new ParticleDefinition
        {
            StartColor = new Vector4(1.0f, 0.8f, 0.2f, 1.0f),
            EndColor = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
            Gravity = new Vector3(0, -3.0f, 0),
            SpeedMinMax = new Vector2(4.0f, 7.0f),
            SizeStartEnd = new Vector2(0.5f, 0.1f),
            LifeMinMax = new Vector2(1.0f, 2.5f)
        };
        var state = new ParticleState
        {
            Translation = new Vector3(0),
            StartArea = new Vector3(0.2f, 0.0f, 0.2f),
            Direction = new Vector3(0, 1, 0),
            Spread = 0.3f
        };

        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "Particle1",
            Transform = new Transform(new Vector3(110, 10, 115), Vector3.One, Quaternion.Identity),
            Components =
            {
                new ParticleBlueprint
                {
                    EmitterName = "Emitter1",
                    ParticleCount = 1024,
                    MaterialId = particleMat.MaterialId,
                    Definition = def,
                    State = state
                }
            },
        });

        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "Particle2",
            Transform = new Transform(new Vector3(120, 8, 120), Vector3.One, Quaternion.Identity),
            Components =
            {
                new ParticleBlueprint
                {
                    EmitterName = "Emitter2",
                    ParticleCount = 1024,
                    MaterialId = particleMat.MaterialId,
                    Definition = ParticleDefinition.MakeDefault(),
                    State = new ParticleState
                    {
                        Translation = new Vector3(0),
                        StartArea = new Vector3(3.0f, 1.5f, 3.0f),
                        Direction = new Vector3(0.01f, 0.01f, 0.01f),
                        Spread = 3.14f
                    }
                }
            },
        });
    }

    private void CreateWarrior(AssetSystem assets)
    {
        var sceneManager = Context.SceneManager;

        var model = assets.Store.GetByName<Model>("Warrior");
        var mat = assets.MaterialStore.Get("Warrior::Materials/0");
        mat.Shininess = 2f;
        mat.Specular = 0.05f;

        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "Warrior0",
            Transform = new Transform(new Vector3(107, 6.2f, 113), new Vector3(2), Quaternion.Identity),
            Components = { new ModelBlueprint(model.Id, mat.MaterialId) }
        });

        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "Warrior1",
            Transform = new Transform(new Vector3(118, 6.2f, 107.5f), new Vector3(2), Quaternion.Identity),
            Components = { new ModelBlueprint(model.Id, mat.MaterialId) }
        });
    }

    private void CreateCesiumMan(AssetSystem assets)
    {
        var sceneManager = Context.SceneManager;

        var model = assets.Store.GetByName<Model>("Cesium_Man");
        var mat = assets.MaterialStore.CreateMaterial("EmptyAnimated", "CesiumMat");

        for (int i = 0; i < 32; i++)
        {
            var bp = new SceneObjectBlueprint
            {
                Name = $"Cesium Man{i}",
                Transform = new Transform(new Vector3(111, 6.3f, 17), new Vector3(1), Quaternion.Identity),
            };

            var transform = new Transform(new Vector3(i * 2, 0, i * 2), new Vector3(2), Quaternion.Identity);
            bp.Components.Add(new ModelBlueprint(model.Id, mat.MaterialId) { LocalTransform = transform });
            sceneManager.CreateSceneObject(bp);
        }
    }

    private void CreateWell(AssetSystem assets)
    {
        var sceneManager = Context.SceneManager;

        var model = assets.Store.GetByName<Model>("Well");
        var mat = assets.MaterialStore.Get("Well::Materials/0");
        var mat1 = assets.MaterialStore.Get("Well::Materials/1");
        var mat2 = assets.MaterialStore.Get("Well::Materials/2");

        mat.Shininess = 2f;
        mat.Specular = 0.05f;

        var transform = new Transform(new Vector3(106f, 6.124f, 117.5f), new Vector3(1),
            Quaternion.CreateFromYawPitchRoll(FloatMath.ToRadians(180), 0, 0));
        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "Well",
            Transform = transform,
            Components = { new ModelBlueprint(model.Id, mat.MaterialId, mat1.MaterialId, mat2.MaterialId) }
        });
    }

    private void CreateForestHut(AssetSystem assets)
    {
        var sceneManager = Context.SceneManager;

        var model = assets.Store.GetByName<Model>("ForestHut");
        var mat = assets.MaterialStore.Get("ForestHut::Materials/0");
        mat.Transparency = true;
        mat.Shininess = 2f;
        mat.Specular = 0.05f;
        mat.Pipeline = new MaterialPipeline
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend, GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassFunctions(BlendMode.Alpha)
        };

        var transform = new Transform(new Vector3(131, 6.124f, 97f), new Vector3(4),
            Quaternion.CreateFromYawPitchRoll(FloatMath.ToRadians(-140), FloatMath.ToRadians(180), 0));
        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "ForestHut", Transform = transform, Components = { new ModelBlueprint(model.Id, mat.MaterialId) }
        });
    }


    private void CreateKnight(AssetSystem assets)
    {
        var sceneManager = Context.SceneManager;

        var model = assets.Store.GetByName<Model>("Knight");
        var mat = assets.MaterialStore.Get("Knight::Materials/0");
        mat.Shininess = 2f;
        mat.Specular = 0.05f;

        var transform = new Transform(new Vector3(110, 6, 125), new Vector3(2),
            Quaternion.CreateFromYawPitchRoll(0, FloatMath.ToRadians(90), 0));

        sceneManager.CreateSceneObject(new SceneObjectBlueprint
        {
            Name = "Knight", Transform = transform, Components = { new ModelBlueprint(model.Id, mat.MaterialId) }
        });
    }

    private void CreateSpawner(AssetSystem assets)
    {
        var (store, materialStore) = (assets.Store, assets.MaterialStore);

        // Trees
        var treeMesh = store.GetByName<Model>("Tree1");
        var treeMesh1 = store.GetByName<Model>("Tree2");
        var treeMesh2 = store.GetByName<Model>("Tree3");

        var treeMat = materialStore.CreateMaterial("TreeBarkMat", "TreeMat1");
        var birchMat = materialStore.Get("TreeBirchBarkMat");

        var leaf1Mat = materialStore.Get("TreeLeaf1Mat");
        var leaf2Mat = materialStore.Get("TreeLeaf2Mat");

        leaf1Mat.Transparency = true;
        leaf1Mat.Color = new Color4(0.55f, 0.85f, 0.45f);
        leaf1Mat.Shininess = 0f;
        leaf1Mat.Specular = 0f;

        leaf2Mat.Transparency = true;
        leaf2Mat.Color = new Color4(0.55f, 0.85f, 0.45f);
        leaf2Mat.Shininess = 0f;
        leaf2Mat.Specular = 0f;


        var leafState =
            GfxPassState.Set(GfxStateFlags.DepthTest | GfxStateFlags.DepthWrite | GfxStateFlags.PolygonOffset,
                disable: GfxStateFlags.Cull);
        var leafFunc = new GfxPassFunctions(Depth: DepthMode.Lequal, Cull: CullMode.FrontCcw,
            PolygonOffset: PolygonOffsetLevel.Slope);
        var leafPipelineState = new MaterialPipeline(leafState, leafFunc);

        leaf1Mat.Pipeline = leafPipelineState;
        leaf2Mat.Pipeline = leafPipelineState;


        // Rocks
        var rockMat = materialStore.Get("Rock1Mat");
        var rockMat2 = materialStore.Get("Rock2Mat");
        rockMat.Shininess = 10f;
        rockMat.Specular = 0.12f;

        rockMat2.Shininess = 24f;
        rockMat2.Specular = 0.25f;

        var rockMesh = store.GetByName<Model>("Rock1");
        var rock2Mesh = store.GetByName<Model>("Rock2");

        // Boat
        var boatMat = materialStore.Get("BoatMat");
        var boatMesh = store.GetByName<Model>("Boat");
        boatMat.Specular = 0;
        boatMat.Shininess = 1;

        var min = treeMesh.Bounds.Min;
        var max = treeMesh.Bounds.Max;
        var bounds = new BoundingBox(new Vector3(min.X + 6, min.Y, min.Z + 6),
            new Vector3(max.X - 6, max.Y, max.Z - 6));
        _spawner = new EntitySpawner(Context.SceneManager, World);


        var transform = new Transform(new Vector3(110, 6, 125), new Vector3(2),
            Quaternion.CreateFromYawPitchRoll(0, FloatMath.ToRadians(90), 0));


        var treeBlueprint = new ModelBlueprint(treeMesh.Id, treeMat.MaterialId, leaf1Mat.MaterialId);
        var birchBlueprint = new ModelBlueprint(treeMesh1.Id, birchMat.MaterialId, leaf2Mat.MaterialId);
        var birch2Blueprint = new ModelBlueprint(treeMesh2.Id, birchMat.MaterialId, leaf2Mat.MaterialId);

        var rockBlueprint1 = new ModelBlueprint(rockMesh.Id, rockMat.MaterialId);
        var rockBlueprint2 = new ModelBlueprint(rock2Mesh.Id, rockMat2.MaterialId);
        var boatBlueprint = new ModelBlueprint(boatMesh.Id, boatMat.MaterialId);


        _spawner.PlaceTreesBasic(16,
        [
            new ScenePlacement("tree", treeBlueprint),
            new ScenePlacement("birch_1", birchBlueprint),
            new ScenePlacement("birch_2", birch2Blueprint)
        ]);

        _spawner.PlaceGroundRocksBasic(64,
            [
                new ScenePlacement("rock", rockBlueprint1, 0.5f),
                new ScenePlacement("rocker", rockBlueprint2, 0.6f)
            ],
            intensity: 0.5f);
        _spawner.PlacePropsRingBasic(128, [new ScenePlacement("boat", boatBlueprint)]);
    }
}