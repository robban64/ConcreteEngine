#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Game.Effects;
using ConcreteEngine.Core.Game.Sprite;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Rendering.Pipeline;
using ConcreteEngine.Core.Rendering.Renderers;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace Demo;

public sealed class DemoScene : GameScene
{
    public override void ConfigureFeatures(IGameSceneFeatureBuilder builder)
    {
        builder.RegisterDrawFeature<TilemapDrawEmitter, TilemapFeature, TilemapDrawData>(0);
        builder.RegisterDrawFeature<SpriteDrawEmitter, SpriteFeature, SpriteFeatureDrawData>(1);
        builder.RegisterDrawFeature<LightEmitter, LightFeature, LightFeatureDrawData>(2);

    }

    public override void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics)
    {
        builder.RegisterRenderer<DrawCommandMesh, SpriteRenderer>(DrawCommandId.Tilemap, DrawCommandTag.SpriteRenderer);
        builder.RegisterRenderer<DrawCommandMesh, SpriteRenderer>(DrawCommandId.Sprite, DrawCommandTag.SpriteRenderer);
        builder.RegisterRenderer<DrawCommandLight, LightRenderer>(DrawCommandId.Effect, DrawCommandTag.LightRenderer);

        builder.RegisterEmitter<TilemapDrawEmitter, TilemapDrawData>(0);
        builder.RegisterEmitter<SpriteDrawEmitter, SpriteFeatureDrawData>(1);
        builder.RegisterEmitter<LightEmitter, LightFeatureDrawData>(2);

        var assets = Context.GetSystem<AssetSystem>();

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

    public override void Initialize(IGraphicsDevice graphics)
    {
        var renderer = Context.GetSystem<RenderSystem>();

        renderer.CreateMaterialFromTemplate("SpriteMaterial");
        renderer.CreateMaterialFromTemplate("TilemapMaterial");
        renderer.CreateMaterialFromTemplate("LightMaterial");
    }

    public override void Unload()
    {
    }
}