#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Descriptors;

#endregion

namespace Demo3D;

public sealed class Demo3DScene : GameScene
{
    private EntitySpawner _spawner = null!;

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<FlyCameraModule>(0);
        builder.RegisterModule<EffectAdjustModule>(1);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        builder.RegisterRender3D(new RenderTargetDescriptor());
    }

    public override void Unload()
    {
    }


    private void CreateTerrain(IAssetSystem assets)
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

    private void CreateSky(IAssetSystem assets)
    {
        var skyboxMaterial = assets.MaterialStore.CreateMaterial("SkyboxMat", "SkyboxMat1");
        skyboxMaterial.State.Pipeline = new MaterialPipelineState(
            GfxPassState.Disable(GfxStateFlags.DepthWrite),
            GfxPassStateFunc.MakeSky());

        Context.World.Sky.SetSkyMaterial(skyboxMaterial.Id);
    }

    private void CreateParticles(IAssetSystem assets)
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
        {
            var emitter = worldParticles.CreateEmitter(1024, ParticleDefinition.MakeDefault());
            worldParticles.SetMaterial(particleMat.Id);
            emitter.MaterialId = particleMat.Id;

            var component = new ParticleComponent(emitter.MeshId, emitter.EmitterHandle, emitter.MaterialId,
                emitter.ParticleCount);
            Context.World.Entities.CreateParticleEntity(emitter.MeshId, component);
        }
        
        {
            var emitter = worldParticles.CreateEmitter(1024, ParticleDefinition.MakeDefault());
            worldParticles.SetMaterial(particleMat.Id);
            emitter.MaterialId = particleMat.Id;

            var component = new ParticleComponent(emitter.MeshId, emitter.EmitterHandle, emitter.MaterialId,
                emitter.ParticleCount);
            Context.World.Entities.CreateParticleEntity(emitter.MeshId, component);
        }

    }

    private void CreateAnimatedEntities(IAssetSystem assets)
    {
        var worldEntities = Context.World.Entities;
        var worldMaterials = Context.World.EntityMaterials;

        for (int i = 0; i < 2; i++)
        {
            var warriorModel = assets.Store.GetByName<Model>("Warrior");
            var warriorMat = assets.MaterialStore.Get("Warrior::Materials/0");
            var warriorMatKey = (MaterialTagBuilder.BuildOne(warriorMat.Id));
            var clip = warriorModel.Animation![0];

            warriorMat.State.Shininess = 2f;
            warriorMat.State.Specular = 0.05f;

            var transform = Transform.Identity with
            {
                Translation = new Vector3(115, 6, 115 + i * 5), Scale = new Vector3(2)
            };
            var entity = worldEntities.CreateModelEntity(warriorModel.ModelId, warriorModel.DrawCount, warriorMatKey,
                in transform, warriorModel.Bounds);
            var animationComponent = new AnimationComponent(warriorModel.ModelId, warriorModel.AnimationId)
            {
                Duration = clip.Duration, Speed = clip.TicksPerSecond
            };
            worldEntities.AddComponent(entity, animationComponent);

            // animationComponent.Slot = Context.World.MeshTable.GetAnimationSlot(knight.ModelId);
        }


        var cesiumModel = assets.Store.GetByName<Model>("Cesium_Man");
        var cesiumMat = assets.MaterialStore.CreateMaterial("EmptyAnimated", "CesiumMat");
        var cesiumMatKey = (MaterialTagBuilder.BuildOne(cesiumMat.Id));
        var cesiumClip = cesiumModel.Animation![0];

        for (int i = 0; i < 12; i++)
        {
            var transform = Transform.Identity with
            {
                Translation = new Vector3(100 + i * 4, 6, 100 + i * 4),
                Rotation = Quaternion.CreateFromYawPitchRoll(0, 0, 0),
                Scale = new Vector3(2)
            };
            var entity = worldEntities.CreateModelEntity(cesiumModel.ModelId, cesiumModel.DrawCount, cesiumMatKey,
                in transform, cesiumModel.Bounds);

            var animationComponent = new AnimationComponent(cesiumModel.ModelId, cesiumModel.AnimationId)
            {
                Duration = cesiumClip.Duration, Speed = cesiumClip.TicksPerSecond
            };
            worldEntities.AddComponent(entity, animationComponent);
        }


        {
            var knight = assets.Store.GetByName<Model>("Knight");
            var knightMat = assets.MaterialStore.Get("Knight::Materials/0");
            knightMat.State.Shininess = 2f;
            knightMat.State.Specular = 0.05f;

            var transform = Transform.Identity with
            {
                Translation = new Vector3(110, 6, 125),
                Rotation = Quaternion.CreateFromYawPitchRoll(0, FloatMath.ToRadians(90), 0),
                Scale = new Vector3(2)
            };

            var knightMatKey =
                (MaterialTagBuilder.Start(knightMat.Id).WithSlot(knightMat.Id).Build());

            var entity = worldEntities.CreateModelEntity(knight.ModelId, knight.DrawCount, knightMatKey,
                in transform, knight.Bounds);
            //var animationComponent = new AnimationComponent(knight.ModelId, 4, 1, 1);
            // animationComponent.Slot = Context.World.MeshTable.GetAnimationSlot(knight.ModelId);
            //worldEntities.Animations.Add(knightEntity, animationComponent);
        }
    }

    private void CreateSpawner(IAssetSystem assets)
    {
        var (store, materialStore) = (assets.Store, assets.MaterialStore);

        // Trees
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


        var treeMesh = store.GetByName<Model>("Tree1");
        var treeMesh1 = store.GetByName<Model>("Tree2");
        var treeMesh2 = store.GetByName<Model>("Tree3");

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

        _spawner = new EntitySpawner(World);

        var treeMatTag = MaterialTagBuilder.Start(treeMat.Id).WithSlot(leaf1Mat.Id, true).Build();
        var birchMatTag = MaterialTagBuilder.Start(birchMat.Id).WithSlot(leaf2Mat.Id, true).Build();
        var rockMat1Tag = MaterialTagBuilder.BuildOne(rockMat.Id);
        var rockMat2Tag = MaterialTagBuilder.BuildOne(rockMat2.Id);
        var boatMatTag = MaterialTagBuilder.BuildOne(boatMat.Id);

        _spawner.PlaceTreesBasic(14,
        [
            new ScenePlacement(treeMesh.ToBaseDrawInfo(), treeMesh.Bounds, treeMatTag),
            new ScenePlacement(treeMesh1.ToBaseDrawInfo(), treeMesh1.Bounds, birchMatTag),
            new ScenePlacement(treeMesh2.ToBaseDrawInfo(), treeMesh2.Bounds, birchMatTag)
        ]);

        _spawner.PlaceGroundRocksBasic(90,
            [
                new ScenePlacement(rockMesh.ToBaseDrawInfo(), rockMesh.Bounds, rockMat1Tag, 0.5f),
                new ScenePlacement(rock2Mesh.ToBaseDrawInfo(), rock2Mesh.Bounds, rockMat2Tag, 0.6f)
            ],
            intensity: 0.5f);
        _spawner.PlacePropsRingBasic(256, [new ScenePlacement(boatMesh.ToBaseDrawInfo(), boatMesh.Bounds, boatMatTag)]);

/*
        {
            var mesh = store.GetByName<Model>("Cube");
            var entityId = World.Entities.Create();
            var mat = World.EntityMaterials.Add(rockMat1Tag);
            World.Entities.Models.Add(entityId, new ModelComponent(mesh.ModelId, mesh.DrawCount, mat));
            World.Entities.Transforms.Add(entityId,
                new TransformComponent(new Vector3(half, worldTerrain.GetSmoothHeight(half, half) + 1f, half),
                    Vector3.One, Quaternion.Identity));
        }
*/
    }

    public override void Initialize()
    {
        var renderer = Context.GetSystem<IWorldRenderer>();
        var assets = Context.GetSystem<IAssetSystem>();

        // Scene globals
        var rb = renderer.WorldRenderParams;
        rb.SetShadowDefault(2048, 120f);


        // Terrain
        CreateTerrain(assets);

        // Skybox
        CreateSky(assets);

        // Particle
        CreateParticles(assets);

        CreateAnimatedEntities(assets);

        CreateSpawner(assets);

        float half = 256 / 2f;
        var worldTerrain = Context.World.Terrain;
        Camera.Translation = new Vector3(half - 30, worldTerrain.GetSmoothHeight(half - 30, half + 30) + 4f, half + 30);
    }
}