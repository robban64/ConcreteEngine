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
        var renderer = Context.GetSystem<IRenderSystem>();
        var assets = Context.GetSystem<IAssetSystem>();

        var skyboxShader = assets.Get<Shader>("Skybox");
        var skyboxCubeMap = assets.Get<CubeMap>("Skybox");

        RenderGlobals.SetSkybox(skyboxShader.ResourceId, skyboxCubeMap.ResourceId, Quaternion.Identity);
        RenderGlobals.SetDirLight(new Vector3(-0.3f, -1.0f, -0.2f), new Vector3(1.0f, 0.95f, 0.9f), Vector3.One,2);
        var boatMat = renderer.CreateMaterial("BoatMat");
        var boatMesh = assets.Get<Mesh>("Boat");

        var rng = Random.Shared;
        for (int i = 0; i < 120; i++)
        {
            var entityId = World.Create();
            var x = rng.Next(0, 20);
            var y = rng.Next(0, 20);
            World.Meshes.Add(entityId,
                new MeshComponent(boatMesh.ResourceId, boatMat.Id, boatMesh.Meta.DrawCount));
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

        builder.RegisterRender3D(new RenderTargetDescriptor
        {
            SceneTarget = new SceneTargetDesc
            {
                ClearColor = Colors.CornflowerBlue,
                Samples = 4
            },
            ScreenTarget = new ScreenTargetDesc
            {
                CompositeShaderId = screenShader.ResourceId
            }
        });
    }

    public override void Unload()
    {
    }
}