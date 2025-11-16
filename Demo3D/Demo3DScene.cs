#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Meshes;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Descriptors;

#endregion

namespace Demo3D;

public sealed class Demo3DScene : GameScene
{
    private EntitySpawner _spawner;

    public override void Initialize()
    {
        var renderer = Context.GetSystem<IWorldRenderer>();
        var assets = Context.GetSystem<IAssetSystem>();
        var (store, materialStore) = (assets.Store, assets.MaterialStore);

        // Scene globals
        var rb = renderer.WorldRenderParams;
        rb.SetShadowDefault(2048, 120f);

        // Skybox
        var skyboxMaterial = materialStore.CreateMaterial("SkyboxMat", "SkyboxMat1");
        skyboxMaterial.State.Pipeline = new MaterialPipelineState(
            GfxPassState.Disable(GfxStateFlags.DepthWrite),
            GfxPassStateFunc.MakeSky());

        Context.World.Sky.SetSkyMaterial(skyboxMaterial.Id);

        // Terrain
        var heightmap = assets.Store.GetByName<Texture2D>("Heightmap");
        var terrainMat = assets.MaterialStore.CreateMaterial("TerrainMat", "TerrainMat1");
        terrainMat.State.UvRepeat = 14;
        terrainMat.State.Shininess = 4;
        terrainMat.State.Specular = 0.02f;

        var worldTerrain = Context.World.Terrain;
        worldTerrain.CreateTerrainMesh(heightmap);
        worldTerrain.SetMaterial(terrainMat.Id);

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

        _spawner.PlaceTreesBasic(20,
        [
            new ScenePlacement(treeMesh.ToBaseDrawInfo(), treeMatTag),
            new ScenePlacement(treeMesh1.ToBaseDrawInfo(), birchMatTag),
            new ScenePlacement(treeMesh2.ToBaseDrawInfo(), birchMatTag)
        ]);

        _spawner.PlaceGroundRocksBasic(90,
            [
                new ScenePlacement(rockMesh.ToBaseDrawInfo(), rockMat1Tag, 0.5f),
                new ScenePlacement(rock2Mesh.ToBaseDrawInfo(), rockMat2Tag, 0.6f)
            ],
            intensity: 0.5f);
        _spawner.PlacePropsRingBasic(12, [new ScenePlacement(boatMesh.ToBaseDrawInfo(), boatMatTag)]);

        float half = 256 / 2f;

        {
            var mesh = store.GetByName<Model>("Cube");
            var entityId = World.Entities.Create();
            var mat = World.EntityMaterials.Add(rockMat1Tag);
            World.Entities.Meshes.Add(entityId, new ModelComponent(mesh.ModelId, mesh.DrawCount, mat));
            World.Entities.Transforms.Add(entityId,
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
        builder.RegisterRender3D(new RenderTargetDescriptor());
    }

    public override void Unload()
    {
    }
}