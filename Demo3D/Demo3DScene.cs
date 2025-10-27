#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Meshes;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Descriptors;
using Shader = ConcreteEngine.Core.Assets.Shaders.Shader;

#endregion

namespace Demo3D;

public sealed class Demo3DScene : GameScene
{
    private EntitySpawner _spawner;

    public override void Initialize()
    {
        var renderer = Context.GetSystem<IRenderingSystem>();
        var assets = Context.GetSystem<IAssetSystem>();
        var (store, materialStore) = (assets.Store, assets.MaterialStore);

        // Scene globals
        var rb = renderer.SceneProperties;
        rb.SetShadowDefault(2048);

        // Skybox
        var skyboxMaterial = materialStore.CreateMaterial("SkyboxMat", "SkyboxMat1");
        skyboxMaterial.IsSkybox = true;
        Context.World.Sky.SetSkyMaterial(skyboxMaterial.Id);

        // Terrain
        var heightmap = assets.Store.GetByName<Texture2D>("Heightmap");
        var terrainMat = assets.MaterialStore.CreateMaterial("TerrainMat", "TerrainMat1");
        terrainMat.State.UvRepeat = 14;
        terrainMat.State.Shininess = 10;
        terrainMat.State.Specular = 0.04f;

        var worldTerrain = Context.World.Terrain;
        worldTerrain.CreateTerrainMesh(heightmap);
        worldTerrain.SetMaterial(terrainMat.Id);

        // Trees
        var treeMat = materialStore.CreateMaterial("TreeBarkMat", "TreeMat1");
        var birchMat = materialStore.CreateMaterial("TreeBirchBarkMat", "TreeMat2");

        var treeMesh = store.GetByName<Model>("Tree1");
        var treeMesh1 = store.GetByName<Model>("Tree2");
        var treeMesh2 = store.GetByName<Model>("Tree3");

        // Rocks
        var rockMat = materialStore.CreateMaterial("Rock1Mat", "Rock1Mat1");
        var rockMat2 = materialStore.CreateMaterial("Rock2Mat", "Rock1Mat2");
        rockMat.State.Specular = 0.3f;
        rockMat2.State.Specular = 0.25f;
        var rockMesh = store.GetByName<Model>("Rock1");
        var rock2Mesh = store.GetByName<Model>("Rock2");

        // Boat
        var boatMat = materialStore.CreateMaterial("BoatMat", "BoatMat1");
        var boatMesh = store.GetByName<Model>("Boat");
        boatMat.State.Specular = 0;
        boatMat.State.Shininess = 1;

        _spawner = new EntitySpawner(World);

        _spawner.PlaceTreesBasic(20,
        [
            new ScenePlacement(treeMesh, treeMat),
            new ScenePlacement(treeMesh1, birchMat),
            new ScenePlacement(treeMesh2, birchMat)
        ]);

        _spawner.PlaceGroundRocksBasic(90,
            [new ScenePlacement(rockMesh, rockMat, 0.5f), new ScenePlacement(rock2Mesh, rockMat2, 0.6f)],
            intensity: 0.5f);
        _spawner.PlacePropsRingBasic(12, [new ScenePlacement(boatMesh, boatMat)]);

        float half = 256 / 2f;

        {
            var mesh = store.GetByName<Model>("Cube");
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new ModelComponent(mesh.RenderId, treeMat.Id, mesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(half, worldTerrain.GetSmoothHeight(half, half) + 1f, half),
                    Vector3.One, Quaternion.Identity));
        }

        Camera.Translation = new Vector3(half - 30, worldTerrain.GetSmoothHeight(half - 30, half + 30) + 4f, half + 30);
    }

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<FlyCameraModule>(0);
        builder.RegisterModule<EffectAdjustModule>(1);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        /*
        var assetStore = Context.GetSystem<IAssetSystem>().Store;
        var screenShader = assetStore.GetByName<Shader>("Screen");
        var compositeShader = assetStore.GetByName<Shader>("Composite");
        var presentShader = assetStore.GetByName<Shader>("Present");
        var colorFilterShader = assetStore.GetByName<Shader>("ColorFilter");
         */

        builder.RegisterRender3D(new RenderTargetDescriptor());
    }

    public override void Unload()
    {
    }
}