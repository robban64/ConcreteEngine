using ConcreteEngine.Core.Engine.Configuration;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;

    protected Camera Camera => CameraManager.Instance.Camera;

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

    internal void Build(GameSceneConfigBuilder builder)
    {
        ConfigureModules(builder);
    }
}