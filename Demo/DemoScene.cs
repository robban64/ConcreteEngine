#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Features.Effects;
using ConcreteEngine.Core.Features.Sprite;
using ConcreteEngine.Core.Features.Terrain;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Rendering.Renderers;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace Demo;

public sealed class DemoScene : GameScene
{
    public override void Initialize()
    {
        var renderer = Context.GetSystem<IRenderSystem>();

        var spriteMaterial = renderer.CreateMaterial("SpriteMaterial");
        var tilemapMaterial = renderer.CreateMaterial("TilemapMaterial");
        renderer.CreateMaterial("LightMaterial");

        var tilemap = SceneNodes.CreateNode<TilemapBehaviour>("tilemap", null, behaviour =>
        {
            behaviour.MaterialId = tilemapMaterial.Id;
        });

        var dummyLightNode = SceneNodes.CreateEmptyNode("LightNodes");
        for (int i = 0; i < 10; i++)
        {
            SceneNodes.CreateNode<LightBehaviour>($"light-{i}", dummyLightNode,
                b => { b.Position = new Vector2(64 * i, 64 * i); });
        }

        var sprite1 = SceneNodes.CreateNode<SpriteBehaviour>("node1", null, behaviour =>
        {
            behaviour.MaterialId = spriteMaterial.Id;
            behaviour.SourceRectangle = new Rectangle<int>(0, 0, 64, 64);
            behaviour.Batched = true;
        });

        var sprite2 = SceneNodes.CreateNode<SpriteBehaviour>("node2", null, behaviour =>
        {
            behaviour.MaterialId = spriteMaterial.Id;
            behaviour.SourceRectangle = new Rectangle<int>(0, 0, 64, 64);
            behaviour.Batched = true;
        });

        sprite1.LocalTransform.Scale = new Vector2(64, 64);
        sprite2.LocalTransform.Scale = new Vector2(64, 64);

        sprite1.LocalTransform.Position = new Vector2(64, 64);
        sprite2.LocalTransform.Position = new Vector2(64, 128);
    }

    protected override void ConfigureFeatures(IGameSceneFeatureBuilder builder)
    {
        builder.RegisterDrawFeature<TilemapDrawEmitter, TilemapFeature, TilemapDrawData>(0);
        builder.RegisterDrawFeature<SpriteDrawEmitter, SpriteFeature, SpriteFeatureDrawData>(1);
        builder.RegisterDrawFeature<LightEmitter, LightFeature, LightFeatureDrawData>(2);
    }

    protected override void ConfigureModules(IGameSceneModuleBuilder builder)
    {
        builder.RegisterModule<RtsCameraModule>(0);
        builder.RegisterModule<NpcSpriteModule>(1);
    }

    protected override void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics)
    {
        builder.RegisterRenderer<DrawCommandMesh, SpriteRenderer>(DrawCommandTag.SpriteRenderer, DrawCommandId.Tilemap,
            DrawCommandId.Sprite);
        builder.RegisterRenderer<DrawCommandLight, LightRenderer>(DrawCommandTag.LightRenderer, DrawCommandId.Effect);

        builder.RegisterEmitter<TilemapDrawEmitter, TilemapDrawData>(0);
        builder.RegisterEmitter<SpriteDrawEmitter, SpriteFeatureDrawData>(1);
        builder.RegisterEmitter<LightEmitter, LightFeatureDrawData>(2);

        var assets = Context.GetSystem<IAssetSystem>();

        var lightPassShader = assets.Get<Shader>("LightPassShader");
        var lightComposite = assets.Get<Shader>("LightComposite");

        // single-sample scene FBO
        var sceneFboId =
            graphics.CreateFramebuffer(new FrameBufferDesc(SizeRatio: Vector2.One, DepthStencilBuffer: true),
                out var sceneFboMeta);

        var lightFboId =
            graphics.CreateFramebuffer(
                new FrameBufferDesc(SizeRatio: new Vector2(0.3f, 0.3f), TexturePreset: TexturePreset.NearestClamp,
                    DepthStencilBuffer: false),
                out var lightFboMeta);


        // colorTexId will be 0 for MSAA
        var msaaFboId = graphics.CreateFramebuffer(new FrameBufferDesc(
            SizeRatio: Vector2.One, DepthStencilBuffer: true, Msaa: true, Samples: 4), out _);

        // Pass 0: draw scene into MSAA FBO
        builder.RegisterRenderPass(RenderTargetId.Scene, 0, new SceneRenderPass
        {
            TargetFbo = msaaFboId,
            Clear = new RenderPassClearDesc(Color.CornflowerBlue, ClearBufferFlag.ColorAndDepth)
        });

        // Pass 1: resolve MSAA into single-sample texture FBO
        builder.RegisterRenderPass(RenderTargetId.Scene, 1, new BlitRenderPass
            {
                TargetFbo = sceneFboId,
                BlitFbo = msaaFboId,
                Multisample = true,
                Samples = 4
            }
        );

        // Pass 2: Draw light into FBO
        //SourceTexId = [lightFboMeta.ColTexId],
        builder.RegisterRenderPass(RenderTargetId.SceneLight, 2, new LightRenderPass
            {
                TargetFbo = lightFboId,
                Shader = lightPassShader.ResourceId,
                Clear = new RenderPassClearDesc(Color.FromArgb(255, 125, 125, 150), ClearBufferFlag.Color),
                Blend = BlendMode.Additive,
                DepthTest = false
            }
        );

        // Pass 3: Combine scene and light fbo texture into final scene
        builder.RegisterRenderPass(RenderTargetId.SceneLight, 3, new FsqRenderPass
        {
            TargetFbo = default,
            SourceTextures = [sceneFboMeta.ColTexId, lightFboMeta.ColTexId],
            Shader = lightComposite.ResourceId
        });
    }

    public override void Unload()
    {
    }
}