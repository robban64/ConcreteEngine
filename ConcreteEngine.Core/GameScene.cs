#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core;

public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;

    public abstract void Configure();
    public abstract void OnReady(IGraphicsDevice graphics);
    public abstract void TickUpdate(int tick);
    public abstract void Unload();

    protected GameScene()
    {
    }


    internal void AttachContext(GameSceneContext context)
    {
        Context = context;
    }
}