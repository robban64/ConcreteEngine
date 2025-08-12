#region

using ConcreteEngine.Core;
using ConcreteEngine.Core.Module;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace Demo;

public class DemoScene : GameScene
{

    protected override void Configure()
    {
        RegisterModule<SpriteModule>();
        RegisterModule<RtsCameraModule>();
    }

    protected override void OnReady()
    {
        var spriteModule = GetModule<SpriteModule>();
        Context.Renderer.RegisterCommand<SpriteDrawCommand>(RenderTargetId.None);
        Context.Renderer.RegisterEmitter(new SpriteDrawCommandEmitter {SpriteModule =  spriteModule});
    }

    protected override void Unload()
    {
    }

}