#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics;
using Shader = ConcreteEngine.Core.Assets.Resources.Shader;

#endregion

namespace Demo3D;

public sealed class Demo3DScene : GameScene
{
    public override void Initialize()
    {
        var rng = Random.Shared;

        var renderer = Context.GetSystem<IRenderSystem>();
        var assets = Context.GetSystem<IAssetSystem>();

        var skyboxMaterial = renderer.CreateMaterial("SkyboxMat");

        var rb = renderer.SceneRenderProps;
        rb.SetSkybox(skyboxMaterial.Id, Quaternion.Identity);
        rb.SetDirLight(
            new Vector3(-0.4f, -1.0f, 0.35f),
            new Vector4(1.00f, 0.95f, 0.88f, 1.15f),
            0.35f
        );
        rb.SetAmbient(new Vector3(0.8f, 0.75f, 0.8f));

        var boatMat = renderer.CreateMaterial("BoatMat");
        var boatMesh = assets.Get<Mesh>("Boat");
        boatMat.SpecularStrength = 0;
        boatMat.Shininess = 1;


        var rockMat = renderer.CreateMaterial("Rock01Mat");
        rockMat.SpecularStrength = 0.3f;
        var rockMesh = assets.Get<Mesh>("Rock1");
        var rock2Mesh = assets.Get<Mesh>("Rock2");

        for (int i = 0; i < 40; i++)
        {
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(rockMesh.ResourceId, rockMat.Id, rockMesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(i * 5, -3, i * 5), Vector3.One, Quaternion.Identity));
        }

        for (int i = 0; i < 40; i++)
        {
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(rockMesh.ResourceId, rockMat.Id, rockMesh.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3((i * 8) % 12, 3, (i * 8) % 12), Vector3.One , Quaternion.Identity));
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
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        var assets = Context.GetSystem<IAssetSystem>();

        var screenShader = assets.Get<Shader>("Screen");
        var compositeShader = assets.Get<Shader>("Composite");
        var presentShader = assets.Get<Shader>("Present");
        var colorFilterShader = assets.Get<Shader>("ColorFilter");


        builder.RegisterRender3D(new RenderTargetDescriptor
        {
            SceneTarget = new SceneTargetDesc
            {
                ClearColor = Color4.CornflowerBlue,
                Samples = 4
            },
            /*LightTarget = new LightTargetDesc
            {
                LightShaderId = lightShader.ResourceId,
                Blend = BlendMode.Additive,
                TexPreset = TexturePreset.LinearMipmapRepeat
            },*/
            PostEffectTarget = new PostEffectTargetDesc
            {
                EffectShaderId = colorFilterShader.ResourceId,
                CompositeShaderId = compositeShader.ResourceId,
            },
            ScreenTarget = new ScreenTargetDesc
            {
                ScreenShaderId = presentShader.ResourceId
            },
        });
    }

    public override void Unload()
    {
    }
}