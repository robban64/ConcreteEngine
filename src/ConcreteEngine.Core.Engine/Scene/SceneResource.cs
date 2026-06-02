using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class SceneResource : IDisposable
{
    public abstract void Dispose();
}
