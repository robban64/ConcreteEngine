#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Scene;

public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;

    public abstract void ConfigureFeatures(IGameSceneFeatureBuilder builder);
    public abstract void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics);
    public abstract void Initialize();
    public abstract void Unload();

    protected GameScene()
    {
    }

    internal void AttachContext(GameSceneContext context)
    {
        Context = context;
    }
}