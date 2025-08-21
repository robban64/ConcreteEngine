#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Game.Sprite;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Resources;
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
        builder.RegisterDrawFeature<TilemapDrawEmitter, TilemapFeature, TilemapStruct>(0);
        builder.RegisterDrawFeature<SpriteDrawEmitter, SpriteFeature, SpriteDrawEntity>(1);
    }

    public override void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics)
    {
        builder.RegisterCommand(RenderTargetId.Scene, DrawCommandId.Tilemap, 4);
        builder.RegisterCommand(RenderTargetId.Scene, DrawCommandId.Sprite, 32);
        builder.RegisterCommand(RenderTargetId.SceneLight, DrawCommandId.Effect, 32);

        builder.RegisterEmitter<TilemapDrawEmitter, TilemapStruct>(0);
        builder.RegisterEmitter<SpriteDrawEmitter, SpriteDrawEntity>(1);

        var renderer = Context.GetSystem<RenderSystem>();
        var assets = Context.GetSystem<AssetSystem>();

        //var spriteShader = assets.Get<Shader>("SpriteShader");
        var screenShader = assets.Get<Shader>("ScreenShader");
        var lightPassShader = assets.Get<Shader>("LightPassShader");
        var lightComposite = assets.Get<Shader>("LightComposite");


        //var spriteTexture = assets.Get<Texture2D>("SpriteTexture");
        //var tilemapTexture = assets.Get<Texture2D>("TilemapTextureAtlas");

        var halfSize = Vector2.One * 0.5f;

        // Create a single-sample texture FBO for post-FX
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
            Clear = new RenderPassClearDesc(Color.Black, ClearBufferFlag.ColorAndDepth),
        });

        // Pass 1: resolve MSAA → single-sample texture FBO
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
                Clear = new RenderPassClearDesc(Color.FromArgb(255, 200, 200, 255), ClearBufferFlag.Color),
                Blend = BlendMode.Additive,
                DepthTest = false
            }
        );

        // Pass 3: Combine scene and light fbo texture into final scene
        builder.RegisterRenderPass(RenderTargetId.SceneLight, 3, new FsqRenderPass
        {
            TargetFbo = default,
            SourceTextures = [sceneFboMeta.ColTexId, lightFboMeta.ColTexId],
            Shader = lightComposite.ResourceId,
        });

    }

    public override void Initialize(IGraphicsDevice graphics)
    {
        var renderer = Context.GetSystem<RenderSystem>();
        var assets = Context.GetSystem<AssetSystem>();

        var spriteShader = assets.Get<Shader>("SpriteShader");
        var screenShader = assets.Get<Shader>("ScreenShader");
        var lightPassShader = assets.Get<Shader>("LightPassShader");
        var lightComposite = assets.Get<Shader>("LightComposite");


        var spriteTexture = assets.Get<Texture2D>("SpriteTexture");
        var tilemapTexture = assets.Get<Texture2D>("TilemapTextureAtlas");

        renderer.CreateMaterialFromTemplate("SpriteMaterial");
        renderer.CreateMaterialFromTemplate("TilemapMaterial");
        renderer.CreateMaterialFromTemplate("LightMaterial");

        /*
        var colorShader = assets.Get<Shader>("ColorShader");
        var blurHorizontalShader = assets.Get<Shader>("BlurHorizontal");
        var blurVerticalShader = assets.Get<Shader>("BlurVertical");
        var brightPassShader = assets.Get<Shader>("BrightPass");
        var screenCompositeShader = assets.Get<Shader>("ScreenComposite");
        */
    }

    public override void Unload()
    {
    }
}