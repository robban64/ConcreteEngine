#region

using System.Drawing;
using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Game.Sprite;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Shader = ConcreteEngine.Core.Resources.Shader;

#endregion

namespace Demo;

public class DemoScene : GameScene
{
    public override void Configure()
    {
        Context.RegisterFeature<TilemapFeature>();
        Context.RegisterFeature<SpriteFeature>();
    }

    public override void OnReady(IGraphicsDevice graphics)
    {
        var renderer = Context.GetSystem<RenderSystem>();
        var assets = Context.GetSystem<AssetSystem>();

        var spriteModule = Context.GetFeature<SpriteFeature>();
        var tilemapFeature = Context.GetFeature<TilemapFeature>();


        var spriteShader = assets.Get<Shader>("SpriteShader");
        var spriteTexture = assets.Get<Texture2D>("SpriteTexture");
        var tilemapTexture = assets.Get<Texture2D>("TilemapTextureAtlas");

        var screenShader = assets.Get<Shader>("ScreenShader");
        var colorShader = assets.Get<Shader>("ColorShader");

        var blurHorizontalShader = assets.Get<Shader>("BlurHorizontal");
        var blurVerticalShader = assets.Get<Shader>("BlurVertical");
        var brightPassShader = assets.Get<Shader>("BrightPass");
        var screenCompositeShader = assets.Get<Shader>("ScreenComposite");

        var halfSize = Vector2.One * 0.5f;


        /*
        renderer.RegisterRenderPass(RenderTargetId.Scene, 0, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: screenFboId,
            ReadTexId: screenTexId,
            ShaderId: screenShader.ResourceId,
            SizeRatio: Vector2.One,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth
        ));
        */

        // Create a single-sample texture FBO for post-FX
        var (sceneFboId, sceneTexId) =
            graphics.CreateFramebuffer(new FramebufferDescriptor(SizeRatio: Vector2.One, DepthStencilBuffer: true));

        // colorTexId will be 0 for MSAA
        var msaaDesc = new FramebufferDescriptor(
            SizeRatio: Vector2.One, DepthStencilBuffer: true, Msaa: true, Samples: 4);
        var (msaaFboId, _) = graphics.CreateFramebuffer(msaaDesc); 


        // Pass 0: draw scene into MSAA FBO
        renderer.RegisterRenderPass(RenderTargetId.Scene, 0, new RenderPassData(
            Op: RenderPassOp.DrawScene,
            TargetFboId: msaaFboId,
            DoClear: true,
            ClearMask: ClearBufferFlag.ColorAndDepth,
            ClearColor: Color.CornflowerBlue,
            Blend: BlendMode.Alpha,
            DepthTest: true));

        // Pass 1: resolve MSAA → single-sample texture FBO
        renderer.RegisterRenderPass(RenderTargetId.Scene, 1, new RenderPassData(
            Op: RenderPassOp.Blit,
            TargetFboId: sceneFboId,
            BlitFboId: msaaFboId));

        // Pass 2: fullscreen color grade to screen
        renderer.RegisterRenderPass(RenderTargetId.Scene, 2, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            TargetFboId: 0,
            SourceTexId: sceneTexId,
            ShaderId: screenShader.ResourceId,
            Blend: BlendMode.None,
            DepthTest: false));


        /*
        renderer.RegisterRenderPass(RenderTargetId.Scene, 0, new RenderPassData(
            Op: RenderPassOp.DrawScene,
            TargetFboId: sceneFboId,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth,
            Blend: BlendMode.Alpha,
            DepthTest: true
        ));

        renderer.RegisterRenderPass(RenderTargetId.Scene, 1, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            TargetFboId: 0,
            SourceTexId: sceneTexId,
            ShaderId: screenShader.ResourceId,
            SizeRatio: Vector2.One,
            Blend: BlendMode.None,
            DepthTest: false
        ));

        renderer.RegisterRenderPass(RenderTargetId.Scene, 2, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            TargetFboId: 0,
            SourceTexId: sceneTexId,
            ShaderId: brightPassShader.ResourceId,
            SizeRatio: Vector2.One,
            Blend: BlendMode.Additive,
            DepthTest: false
        ));
        */

        renderer.AddMaterial(new MaterialDescription(
            Shader: spriteShader,
            Texture: spriteTexture,
            Blend: BlendMode.Alpha
        ));

        renderer.AddMaterial(new MaterialDescription(
            Shader: spriteShader,
            Texture: tilemapTexture,
            Blend: BlendMode.Alpha
        ));


        renderer.RegisterCommand(0, DrawCommandId.Tilemap, RenderTargetId.Scene, 4);
        renderer.RegisterCommand(1, DrawCommandId.Sprite, RenderTargetId.Scene, 32);

        renderer.RegisterEmitter(0, new TilemapDrawEmitter { Tilemap = tilemapFeature });
        renderer.RegisterEmitter(1, new SpriteDrawEmitter { SpriteFeature = spriteModule });
    }

    public override void TickUpdate(int tick)
    {
    }

    public override void Unload()
    {
    }
}