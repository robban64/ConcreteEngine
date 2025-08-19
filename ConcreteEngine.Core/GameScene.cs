#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core;



public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;


    public abstract void ConfigureFeatures(IGameSceneFeatureBuilder builder);
    public abstract void ConfigureRenderer(IGameSceneRenderBuilder builder);
    public abstract void Initialize(IGraphicsDevice graphics);
    public abstract void Unload();

    protected GameScene()
    {
    }


    internal void AttachContext(GameSceneContext context)
    {
        Context = context;
    }
}