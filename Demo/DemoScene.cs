#region

using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Game.Sprite;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

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

    public override void TickUpdate(int tick)
    {
    }

    public override void Unload()
    {
    }
}