#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
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
    public override void Initialize()
    {
        var rng = Random.Shared;

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

        Context.World.Terrain.CreateTerrainMesh(heightmap);
        Context.World.Terrain.SetMaterial(terrainMat.Id);
        


        var treeMat = materialStore.CreateMaterial("TreeBarkMat", "TreeMat1");
        var birchMat = materialStore.CreateMaterial("TreeBirchBarkMat", "TreeMat2");

        var treeMesh = store.GetByName<Mesh>("Tree1");
        var treeMesh1 = store.GetByName<Mesh>("Tree2");
        var treeMesh2 = store.GetByName<Mesh>("Tree3");
        
        {
            var mesh = store.GetByName<Mesh>("Cube");
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(mesh.ResourceId, treeMat.Id, mesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(-50, 0, -50), Vector3.One, Quaternion.Identity));

        }


        int swap = 0;
        for (int i = 0; i < 6; i++)
        {
            var (mesh, mat) = swap switch
            {
                0 => (treeMesh,treeMat),
                1 => (treeMesh1,birchMat),
                _ => (treeMesh2, birchMat)
            };
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(mesh.ResourceId, mat.Id, mesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3( (i * 4) -100, 0,  (i * 4) -100), Vector3.One, Quaternion.Identity));

            if (swap++ > 2) swap = 0;
        }


        // Entities
        var boatMat = materialStore.CreateMaterial("BoatMat", "BoatMat1");
        var boatMesh = store.GetByName<Mesh>("Boat");
        boatMat.State.Specular = 0;
        boatMat.State.Shininess = 1;


        var rockMat = materialStore.CreateMaterial("Rock1Mat", "Rock1Mat1");
        rockMat.State.Specular = 0.3f;
        var rockMesh = store.GetByName<Mesh>("Rock1");
        var rock2Mesh = store.GetByName<Mesh>("Rock2");

        for (int i = 0; i < 40; i++)
        {
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(rockMesh.ResourceId, rockMat.Id, rockMesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(i * 5, -3, i * 5), Vector3.One, Quaternion.Identity));
        }

        var rnd = new Random(9999);
        for (int i = 0; i < 40; i++)
        {
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(rockMesh.ResourceId, rockMat.Id, rockMesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(rnd.Next(0, 48), rnd.Next(0, 3), rnd.Next(0, 48)),
                    new Vector3(rnd.Next(1, 2)), Quaternion.Identity));
        }


        for (int i = 0; i < 12; i++)
        {
            var entityId = World.Create();
            var x = rng.Next(0, 20);
            var y = rng.Next(0, 20);
            World.Meshes.Add(entityId,
                new MeshComponent(boatMesh.ResourceId, boatMat.Id, boatMesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(x, 0, y), Vector3.One, Quaternion.Identity));
        }
    }

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<FlyCameraModule>(0);
        builder.RegisterModule<EffectAdjustModule>(1);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        var assetStore = Context.GetSystem<IAssetSystem>().Store;

        var screenShader = assetStore.GetByName<Shader>("Screen");
        var compositeShader = assetStore.GetByName<Shader>("Composite");
        var presentShader = assetStore.GetByName<Shader>("Present");
        var colorFilterShader = assetStore.GetByName<Shader>("ColorFilter");


        builder.RegisterRender3D(new RenderTargetDescriptor
        {
            SceneTarget = new SceneTargetDesc { ClearColor = Color4.CornflowerBlue, Samples = 4 },
            /*LightTarget = new LightTargetDesc
            {
                LightShaderId = lightShader.ResourceId,
                Blend = BlendMode.Additive,
                TexPreset = TexturePreset.LinearMipmapRepeat
            },*/
            PostEffectTarget = new PostEffectTargetDesc
            {
                EffectShaderId = colorFilterShader.ResourceId, CompositeShaderId = compositeShader.ResourceId,
            },
            ScreenTarget = new ScreenTargetDesc { ScreenShaderId = presentShader.ResourceId },
        });
    }

    public override void Unload()
    {
    }
}