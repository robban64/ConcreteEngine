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
        
        var (albedoFboId, albedoTexId) = graphics.CreateFramebuffer(Vector2.One);
        var (brightPassFboId, brightPassTexId) = graphics.CreateFramebuffer(halfSize);
        var (bloomAFboId, bloomATexId) = graphics.CreateFramebuffer(Vector2.One);
        var (bloomBFboId, bloomBTexId) = graphics.CreateFramebuffer(Vector2.One);

        renderer.RegisterRenderPass(RenderTargetId.Scene, 0, new RenderPassData(
            Op: RenderPassOp.DrawScene,
            WriteFboId: albedoFboId,
            SizeRatio: Vector2.One,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth
        ));

         renderer.RegisterRenderPass(RenderTargetId.Scene, 1, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: brightPassFboId,
            ReadTexId: albedoTexId,
            ShaderId: brightPassShader.ResourceId,
            SizeRatio: halfSize,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));
         
        renderer.RegisterRenderPass(RenderTargetId.Scene, 2, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: bloomAFboId,
            ReadTexId: brightPassTexId,
            ShaderId: screenShader.ResourceId,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));
        
        renderer.RegisterRenderPass(RenderTargetId.Scene, 3, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: bloomBFboId,
            ReadTexId: bloomATexId,
            ShaderId: blurVerticalShader.ResourceId,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));
        
        renderer.RegisterRenderPass(RenderTargetId.Scene, 4, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: 0,
            ReadTexId: albedoTexId,
            ShaderId: screenCompositeShader.ResourceId,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));

        /*
        renderer.RegisterRenderPass(RenderTargetId.Scene, 1, new RenderPassData(
            Op: RenderPassOp.Blit,
            WriteFboId: albedoFboId,
            BlitFboId: 0,
            SizeRatio: Vector2.One,
            DoClear: false,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));
        */

        /*
        renderer.RegisterRenderPass(RenderTargetId.Scene, 0, new RenderPassData(
            Op: RenderPassOp.None,
            WriteFboId: screenFboId,
            SizeRatio: Vector2.One,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth
        ));
        
        // Bright-pass
        renderer.RegisterRenderPass(RenderTargetId.Scene, 1, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: bloomFboId,
            ReadTexId: bloomTextId,
            ShaderId: brightPassShader.ResourceId,
            SizeRatio: halfSize,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));
        renderer.RegisterRenderPass(RenderTargetId.Scene, 2, new RenderPassData(
            Op: RenderPassOp.FullscreenQuad,
            WriteFboId: screenFboId,
            ReadTexId: screenTexId,
            ShaderId: screenCompositeShader.ResourceId,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));
        */
/*
        var screenKey = renderer.RegisterRenderPass(screenShader, new RegisterRenderTargetDesc(
            Target: RenderTargetId.Scene,
            Order: 0,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth
        ));

        // Bright-pass
        var brightPKey = renderer.RegisterDrawRenderPass(screenKey, brightPassShader, new RegisterRenderTargetDesc(
            Target: RenderTargetId.Scene,
            Order: 1,
            SizeRatio: halfSize,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));

        // Blur horizontal
        var blurHKey = renderer.RegisterDrawRenderPass(brightPKey, blurHorizontalShader, new RegisterRenderTargetDesc(
            Target: RenderTargetId.Scene,
            Order: 2,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));

        // Blur vertical
        var blurVKey = renderer.RegisterDrawRenderPass(blurHKey, blurVerticalShader, new RegisterRenderTargetDesc(
            Target: RenderTargetId.Scene,
            Order: 3,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
        ));

        renderer.RegisterDrawRenderPass(new RenderTargetKey(0), screenCompositeShader, new RegisterRenderTargetDesc(
            Target: RenderTargetId.Scene,
            Order: 4,
            SizeRatio: Vector2.One,
            DoClear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color
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