#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;

#endregion

namespace ConcreteEngine.Core.Scene;

public interface IEntityRegistry
{
}

public abstract class GameScene
{
    private World _world = null!;

    protected GameSceneContext Context { get; private set; } = null!;
    
    public SceneRenderGlobals RenderGlobals { get; } = new();

    protected World World => _world;

    protected GameScene()
    {
    }

    internal void Update(in FrameMetaInfo frameInfo)
    {
        Context.Features.Update(in frameInfo);
        Context.Modules.Update(in frameInfo);
    }

    internal void UpdateTick(int tick)
    {
        Context.Modules.GameTickUpdate(tick);
        Context.Features.GameTickUpdate(tick);
        World.Cleanup();
        RenderGlobals.Commit();
    }


    internal void AttachContext(GameSceneContext context)
    {
        _world = new World(RenderGlobals);
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
        var modules = Context.Modules.Modules;
        foreach (var module in modules)
        {
            module.OnSceneReady();
        }
    }

    protected abstract void ConfigureModules(IGameSceneModuleBuilder builder);
    protected abstract void ConfigureRenderer(IGameSceneRenderBuilder builder);
    

    public abstract void Initialize();
    public abstract void Unload();
}