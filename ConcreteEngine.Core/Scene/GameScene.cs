#region

using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering;

#endregion

namespace ConcreteEngine.Core.Scene;

public interface IEntityRegistry
{
}

public abstract class GameScene
{
    private World _world = null!;

    protected GameSceneContext Context { get; private set; } = null!;

    protected World World => _world;

    protected GameScene()
    {
    }

    internal void Update(in UpdateTickInfo frameCtx)
    {
        Context.Features.Update(in frameCtx);
        Context.Modules.Update(in frameCtx);
    }

    internal void UpdateTick(int tick)
    {
        Context.Modules.GameTickUpdate(tick);
        Context.Features.GameTickUpdate(tick);
        World.Cleanup();
    }


    internal void AttachContext(GameSceneContext context)
    {
        var renderer = context.GetSystem<IRenderSystem>();
        _world = new World(renderer.SceneRenderProps);
        context.World = World;
        Context = context;
    }

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