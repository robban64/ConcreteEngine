using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;

    protected World World => Context.World;
    protected Camera Camera => World.Camera;

    protected GameScene()
    {
    }

    internal void AttachContext(GameSceneContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Context is not null) throw new InvalidOperationException();
        Context = context;
    }

    public abstract void Update(float deltaTime);
    public abstract void UpdateTick(float deltaTime);

    public abstract void Initialize();
    public abstract void Unload();

    protected abstract void ConfigureModules(IGameSceneModuleBuilder builder);
    protected abstract void ConfigureRenderer(IGameSceneRenderBuilder builder);


    internal void Build(GameSceneConfigBuilder builder)
    {
        ConfigureRenderer(builder);
        ConfigureModules(builder);
    }
}