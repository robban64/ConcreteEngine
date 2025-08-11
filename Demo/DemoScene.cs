#region

using ConcreteEngine.Core;
using ConcreteEngine.Core.Module;
using Silk.NET.Maths;

#endregion

namespace Demo;

public class DemoScene : GameScene
{
    protected override void Load()
    {
        RegisterModule<SpriteModule>();
        RegisterModule<RtsCameraModule>();

    }

    protected override void Unload()
    {
    }

}