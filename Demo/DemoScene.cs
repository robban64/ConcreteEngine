#region

using System.Drawing;
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

    public override void OnReady()
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

        var vSize = new Vector2D<int>(1280, 720);
        var halfSize = new Vector2D<int>(vSize.X / 2, vSize.Y / 2);
        renderer.RegisterRenderPass("Scene", screenShader, new RegisterRenderTargetDesc(
            Target: RenderTargetId.Scene,
            Order: 0,
            SizeRatio: Vector2D<float>.One,
            DoClear: true,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth,
            ResolveTo: RenderPassResolveTarget.FullscreenQuad,
            ResolveToTarget: new RenderTargetKey(0)
        ));
        
/*
        // Scene: default render
        renderer.RegisterRenderPass("DefaultPass",null, new CreateRenderPassDesc(
            Target: RenderTargetId.Scene,
            Order: 0,
            Size: vSize,
            Clear: true,
            ClearColor: Color.CornflowerBlue,
            ClearMask: ClearBufferFlag.ColorAndDepth,
            ResolveTo: RenderPassResolveTarget.Blit
        ));

        // Bright-pass
        renderer.RegisterRenderPass(brightPassShader, new CreateRenderPassDesc(
            Target: RenderTargetId.Scene,
            Order: 1,
            Size: halfSize,
            Clear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color,
            ResolveTo: RenderPassResolveTarget.FullscreenQuad,
            ResolveToFboId: 0 //TODO
        ));

        // Blur horizontal
        renderer.RegisterRenderPass(blurHorizontalShader,new CreateRenderPassDesc(
            Target: RenderTargetId.Scene,
            Order: 2,
            Size: halfSize,
            Clear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color,
            ResolveTo: RenderPassResolveTarget.FullscreenQuad,
            ResolveToFboId: 0 //TODO
        ));
        
        // Blur vertical
        renderer.RegisterRenderPass(blurVerticalShader,new CreateRenderPassDesc(
            Target: RenderTargetId.Scene,
            Order: 3,
            Size: halfSize,
            Clear: true,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color,
            ResolveTo: RenderPassResolveTarget.FullscreenQuad,
            ResolveToFboId: 0 //TODO
        ));
        
        // Final composite: Scene + Bloom + Vignette → screen
        renderer.RegisterRenderPass(screenCompositeShader,new CreateRenderPassDesc(
            Target: RenderTargetId.Scene,
            Order: 4,
            Size: vSize,
            Clear: false,
            ClearColor: default,
            ClearMask: ClearBufferFlag.Color,
            ResolveTo: RenderPassResolveTarget.FullscreenQuad,
            ResolveToFboId: 0 //TODO
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