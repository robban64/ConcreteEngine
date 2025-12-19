using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Scene.Template;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Shared.World;

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

        // Terrain
        CreateTerrain(assets);

        // Skybox
        CreateSky(assets);

        // Particle
        CreateParticles(assets);

        CreateCesiumMan(assets);

        CreateKnight(assets);
        CreateWarrior(assets);

        CreateSpawner(assets);
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
        var heightmap = assets.Store.GetByName<Texture2D>("Heightmap");
        var terrainMat = assets.MaterialStore.CreateMaterial("TerrainMat", "TerrainMat1");
        terrainMat.State.UvRepeat = 14;
        terrainMat.State.Shininess = 4;
        terrainMat.State.Specular = 0.02f;

        var worldTerrain = Context.World.Terrain;
        worldTerrain.CreateTerrainMesh(heightmap);
        worldTerrain.SetMaterial(terrainMat.Id);
    }

    private void CreateSky(AssetSystem assets)
    {
        var skyboxMaterial = assets.MaterialStore.CreateMaterial("SkyboxMat", "SkyboxMat1");
        skyboxMaterial.State.Pipeline = new MaterialPipelineState(
            GfxPassState.Disable(GfxStateFlags.DepthWrite),
            GfxPassStateFunc.MakeSky());

        Context.World.Sky.SetSkyMaterial(skyboxMaterial.Id);
    }

    private void CreateParticles(AssetSystem assets)
    {
        var particleMat = assets.MaterialStore.CreateMaterial("ParticleMat", "ParticleMat1");
        particleMat.State.Transparency = true;
        particleMat.State.Color = new Color4(0.55f, 0.85f, 0.45f);
        particleMat.State.Shininess = 0f;
        particleMat.State.Specular = 0f;

        particleMat.State.Pipeline = new MaterialPipelineState
        {
            PassState = GfxPassState.Set(GfxStateFlags.Blend,
                GfxStateFlags.DepthWrite | GfxStateFlags.SampleAlphaCoverage),
            PassFunctions = new GfxPassStateFunc(BlendMode.Alpha)
        };

        var worldParticles = Context.World.Particles;
        worldParticles.SetMaterial(particleMat.Id);

        var def = new ParticleDefinition
        {
            StartColor = new Vector4(1.0f, 0.8f, 0.2f, 1.0f),
            EndColor = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
            Gravity = new Vector3(0, -3.0f, 0),
            SpeedMinMax = new Vector2(4.0f, 7.0f),
            SizeStartEnd = new Vector2(0.5f, 0.1f),
            LifeMinMax = new Vector2(1.0f, 2.5f)
        };
        var state = new ParticleEmitterState
        {
            Translation = new Vector3(120, 8, 120),
            StartArea = new Vector3(0.2f, 0.0f, 0.2f),
            Direction = new Vector3(0, 1, 0),
            Spread = 0.3f
        };

        var t1 = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = ParticleComponent.DefaultParticleBounds },
                Particle = new RenderParticleTemplate(in def, in state)
                {
                    EmitterName = "Emitter1",
                    ParticleCount = 1024,
                    Material = particleMat.Id,
                }
            }
        };
        var t2 = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = ParticleComponent.DefaultParticleBounds },
                Particle = new RenderParticleTemplate(ParticleDefinition.MakeDefault(), in state)
                {
                    EmitterName = "Emitter2",
                    ParticleCount = 1024,
                    Material = particleMat.Id,
                }
            }
        };


        var sceneWorld = Context.SceneWorld;

        var particleObj1 = sceneWorld.CreateSceneObject("Particle1");
        var entity1 = sceneWorld.SpawnEntity(particleObj1, t1);
        sceneWorld.GetEntityTransform(entity1.RenderEntityId).Translation = new Vector3(100, 8, 110);


        var particleObj2 = sceneWorld.CreateSceneObject("Particle2");
        var entity2 = sceneWorld.SpawnEntity(particleObj2, t2);
        sceneWorld.GetEntityTransform(entity2.RenderEntityId).Translation = new Vector3(110, 8, 110);
    }

    private void CreateWarrior(AssetSystem assets)
    {
        var sceneWorld = Context.SceneWorld;

        var model = assets.Store.GetByName<Model>("Warrior");
        var mat = assets.MaterialStore.Get("Warrior::Materials/0");
        mat.State.Shininess = 2f;
        mat.State.Specular = 0.05f;

        var template = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = model.Bounds },
                Model = new RenderModelTemplate { Model = model.ModelId, Materials = [mat.GetMeta()] },
                Animation = new RenderAnimationTemplate(model.Animation!)
            }
        };
        for (int i = 0; i < 2; i++)
        {
            var sceneObject = sceneWorld.CreateSceneObject($"Warrior {i}");
            var entity = sceneWorld.SpawnEntity(sceneObject, template);
            ref var entityTransform = ref sceneWorld.GetEntityTransform(entity.RenderEntityId);
            entityTransform.Translation = new Vector3(115, 6, 115 + i * 5);
            entityTransform.Scale = new Vector3(2);
        }
    }

    private void CreateCesiumMan(AssetSystem assets)
    {
        var sceneWorld = Context.SceneWorld;

        var cesiumModel = assets.Store.GetByName<Model>("Cesium_Man");
        var cesiumMat = assets.MaterialStore.CreateMaterial("EmptyAnimated", "CesiumMat");
        var template = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = cesiumModel.Bounds },
                Model = new RenderModelTemplate
                {
                    Model = cesiumModel.ModelId, Materials = [cesiumMat.GetMeta()]
                },
                Animation = new RenderAnimationTemplate(cesiumModel.Animation!)
            }
        };

        var sceneObject = sceneWorld.CreateSceneObject("Cesium Man");

        for (int i = 0; i < 16; i++)
        {
            var entity = sceneWorld.SpawnEntity(sceneObject, template);
            ref var entityTransform = ref sceneWorld.GetEntityTransform(entity.RenderEntityId);
            entityTransform.Translation = new Vector3(100 + i * 4, 6, 100 + i * 4);
            entityTransform.Rotation = Quaternion.CreateFromYawPitchRoll(0, 0, 0);
            entityTransform.Scale = new Vector3(2);
        }
    }

    private void CreateKnight(AssetSystem assets)
    {
        var knight = assets.Store.GetByName<Model>("Knight");
        var knightMat = assets.MaterialStore.Get("Knight::Materials/0");
        knightMat.State.Shininess = 2f;
        knightMat.State.Specular = 0.05f;


        var template = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = knight.Bounds },
                Model = new RenderModelTemplate { Model = knight.ModelId, Materials = [knightMat.GetMeta()] }
            }
        };


        var sceneObject = Context.SceneWorld.CreateSceneObject("Knight");
        var entity = Context.SceneWorld.SpawnEntity(sceneObject, template);

        ref var entityTransform = ref Context.SceneWorld.GetEntityTransform(entity.RenderEntityId);
        entityTransform.Translation = new Vector3(110, 6, 125);
        entityTransform.Rotation = Quaternion.CreateFromYawPitchRoll(0, FloatMath.ToRadians(90), 0);
        entityTransform.Scale = new Vector3(2);
    }

    private void CreateSpawner(AssetSystem assets)
    {
        var (store, materialStore) = (assets.Store, assets.MaterialStore);

        // Trees
        var treeMesh = store.GetByName<Model>("Tree1");
        var treeMesh1 = store.GetByName<Model>("Tree2");
        var treeMesh2 = store.GetByName<Model>("Tree3");

        var treeMat = materialStore.CreateMaterial("TreeBarkMat", "TreeMat1");
        var birchMat = materialStore.CreateMaterial("TreeBirchBarkMat", "TreeMat2");

        var leaf1Mat = materialStore.CreateMaterial("TreeLeaf1Mat", "Leaf1");
        var leaf2Mat = materialStore.CreateMaterial("TreeLeaf2Mat", "Leaf2");

        leaf1Mat.State.Transparency = true;
        leaf1Mat.State.Color = new Color4(0.55f, 0.85f, 0.45f);
        leaf1Mat.State.Shininess = 0f;
        leaf1Mat.State.Specular = 0f;

        leaf2Mat.State.Transparency = true;
        leaf2Mat.State.Color = new Color4(0.55f, 0.85f, 0.45f);
        leaf2Mat.State.Shininess = 0f;
        leaf2Mat.State.Specular = 0f;


        var leafState =
            GfxPassState.Set(GfxStateFlags.DepthTest | GfxStateFlags.DepthWrite | GfxStateFlags.PolygonOffset,
                disable: GfxStateFlags.Cull);
        var leafFunc = new GfxPassStateFunc(Depth: DepthMode.Lequal, Cull: CullMode.FrontCcw,
            PolygonOffset: PolygonOffsetLevel.Slope);
        var leafPipelineState = new MaterialPipelineState(leafState, leafFunc);

        leaf1Mat.State.Pipeline = leafPipelineState;
        leaf2Mat.State.Pipeline = leafPipelineState;


        // Rocks
        var rockMat = materialStore.CreateMaterial("Rock1Mat", "Rock1Mat1");
        var rockMat2 = materialStore.CreateMaterial("Rock2Mat", "Rock1Mat2");
        rockMat.State.Shininess = 10f;
        rockMat.State.Specular = 0.12f;

        rockMat2.State.Shininess = 24f;
        rockMat2.State.Specular = 0.25f;

        var rockMesh = store.GetByName<Model>("Rock1");
        var rock2Mesh = store.GetByName<Model>("Rock2");

        // Boat
        var boatMat = materialStore.CreateMaterial("BoatMat", "BoatMat1");
        var boatMesh = store.GetByName<Model>("Boat");
        boatMat.State.Specular = 0;
        boatMat.State.Shininess = 1;

        _spawner = new EntitySpawner(Context.SceneWorld, World);

        var treeTemplate = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = treeMesh.Bounds },
                Model = new RenderModelTemplate
                {
                    Model = treeMesh.ModelId, Materials = [treeMat.GetMeta(), leaf1Mat.GetMeta()]
                },
            }
        };


        var birchTemplate1 = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = treeMesh1.Bounds },
                Model = new RenderModelTemplate
                {
                    Model = treeMesh1.ModelId, Materials = [birchMat.GetMeta(), leaf2Mat.GetMeta()]
                },
            }
        };

        var birchTemplate2 = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = treeMesh2.Bounds },
                Model = new RenderModelTemplate
                {
                    Model = treeMesh2.ModelId, Materials = [birchMat.GetMeta(), leaf2Mat.GetMeta()]
                },
            }
        };


        var rockTemplate1 = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = rockMesh.Bounds },
                Model = new RenderModelTemplate { Model = rockMesh.ModelId, Materials = [rockMat.GetMeta()] },
            }
        };


        var rockTemplate2 = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = rock2Mesh.Bounds },
                Model = new RenderModelTemplate { Model = rock2Mesh.ModelId, Materials = [rockMat2.GetMeta()] },
            }
        };

        var boatTemplate = new EntityTemplate
        {
            RenderEntity = new RenderEntityTemplate
            {
                Spatial = new SpatialTemplate { LocalBounds = boatMesh.Bounds },
                Model = new RenderModelTemplate { Model = boatMesh.ModelId, Materials = [boatMat.GetMeta()] },
            }
        };

        _spawner.PlaceTreesBasic(14,
        [
            new ScenePlacement("tree", treeTemplate),
            new ScenePlacement("birch_1", birchTemplate1),
            new ScenePlacement("birch_2", birchTemplate2)
        ]);

        _spawner.PlaceGroundRocksBasic(90,
            [
                new ScenePlacement("rock", rockTemplate1, 0.5f),
                new ScenePlacement("rocker", rockTemplate2, 0.6f)
            ],
            intensity: 0.5f);
        _spawner.PlacePropsRingBasic(256, [new ScenePlacement("boat", boatTemplate)]);
    }
}