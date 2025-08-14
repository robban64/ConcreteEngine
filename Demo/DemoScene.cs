#region

using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Game.Camera;
using ConcreteEngine.Core.Game.SpriteBatch;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace Demo;

public class DemoScene : GameScene
{
    protected override void Configure()
    {
        RegisterFeature<TilemapFeature>();
        RegisterFeature<SpriteFeature>();
        RegisterFeature<RtsCameraFeature>();
    }

    protected override void OnReady()
    {
        var spriteModule = GetFeature<SpriteFeature>();
        var tilemapFeature = GetFeature<TilemapFeature>();

        var renderer = Context.Renderer;
        var assets = Context.Assets;

        var spriteShader = Context.Assets.Get<Shader>("SpriteShader");
        var spriteTexture = Context.Assets.Get<Texture2D>("SpriteTexture");
        var tilemapTexture = Context.Assets.Get<Texture2D>("TilemapTextureAtlas");

        
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
        
        renderer.RegisterCommand(0, DrawCommandId.Tilemap, RenderTargetId.None, 4);
        renderer.RegisterCommand(1, DrawCommandId.Sprite, RenderTargetId.None, 32);

        renderer.RegisterEmitter(0, new TilemapDrawEmitter { Tilemap = tilemapFeature });
        renderer.RegisterEmitter(1, new SpriteDrawEmitter { SpriteFeature = spriteModule });
    }

    protected override void Unload()
    {
    }
}