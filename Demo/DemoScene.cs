#region

using ConcreteEngine.Core;
using ConcreteEngine.Core.Game.Camera;
using ConcreteEngine.Core.Game.SpriteBatch;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Rendering;
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

        Context.Renderer.RegisterCommand<TilemapDrawCommand>(RenderTargetId.None);
        Context.Renderer.RegisterCommand<SpriteDrawCommand>(RenderTargetId.None);
        Context.Renderer.RegisterEmitter(0,new TilemapDrawEmitter {Tilemap =  tilemapFeature});
        Context.Renderer.RegisterEmitter(1,new SpriteDrawEmitter {SpriteFeature =  spriteModule});
        
    }

    protected override void Unload()
    {
    }

}