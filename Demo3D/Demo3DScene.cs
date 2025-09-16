#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using Silk.NET.Maths;
using Shader = ConcreteEngine.Core.Resources.Shader;

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

        var rb = renderer.RenderGlobals;
        rb.SetSkybox(skyboxMaterial.Id, Quaternion.Identity);
        rb.SetDirLight(new Vector3(-0.3f, -1.0f, -0.2f), new Vector3(1.0f, 0.95f, 0.9f), Vector3.One,1);
        rb.SetAmbient(new Vector3(0.8f,0.75f,0.8f));

        var rockMat = renderer.CreateMaterial("Rock01Mat");
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

        
        
        var boatMat = renderer.CreateMaterial("BoatMat");
        var boatMesh = assets.Get<Mesh>("Boat");

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

        var lightShader = assets.Get<Shader>("LightPass");
        var colorFilterShader = assets.Get<Shader>("ColorFilter");

        
        builder.RegisterRender3D(new RenderTargetDescriptor
        {
            SceneTarget = new SceneTargetDesc
            {
                ClearColor = Colors.CornflowerBlue,
                Samples = 4
            },
            LightTarget = new LightTargetDesc
            {
                LightShaderId = lightShader.ResourceId,
                Blend = BlendMode.Additive,
                TexPreset = TexturePreset.LinearMipmapRepeat
            },
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