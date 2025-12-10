#region

using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.View;

#endregion

namespace ConcreteEngine.Engine.Scene;

//TODO rework
public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;

    protected IWorld World => Context.World;
    protected Camera3D Camera => World.Camera;


    protected GameScene()
    {
    }

    internal void UpdateTick(float deltaTime)
    {
        Context.Modules.GameTickUpdate(deltaTime);
    }

    internal void AttachContext(GameSceneContext context) => Context = context;

    internal void Build(GameSceneConfigBuilder builder)
    {
        ConfigureRenderer(builder);
        ConfigureModules(builder);
    }

    internal void InitializeInternal()
    {
        Initialize();
    }

    protected abstract void ConfigureModules(IGameSceneModuleBuilder builder);
    protected abstract void ConfigureRenderer(IGameSceneRenderBuilder builder);


    public abstract void Initialize();
    public abstract void Unload();
}