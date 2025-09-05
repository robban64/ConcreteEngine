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
        
        var skyboxShader = assets.Get<Shader>("SkyboxShader");
        var skyboxCubeMap = assets.Get<CubeMap>("Skybox");
        
        RenderGlobals.SetSkybox(skyboxShader.ResourceId, skyboxCubeMap.ResourceId, Quaternion.Identity);

        var boatMat = renderer.CreateMaterial("BoatMat");
        var boatMesh = assets.Get<Mesh>("BoatMesh");

        {
            var entityId = World.Create();
            World.Meshes.Add(entityId,
                new MeshComponent(boatMesh.ResourceId, boatMat.Id, boatMesh.Meta.DrawCount));
            World.Transforms.Add(entityId,
                new Transform(new Vector3(0,0,-10), Vector3.One, Quaternion.Identity));
        }

    }

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<FlyCameraModule>(0);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder)
    {
        var assets = Context.GetSystem<IAssetSystem>();

        var screenShader = assets.Get<Shader>("ScreenShader");

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