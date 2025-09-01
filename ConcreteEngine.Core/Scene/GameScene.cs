#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public interface IEntityRegistry
{
}

public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;
    protected World World { get; } = new();

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
    }


    internal void AttachContext(GameSceneContext context)
    {
        context.World = World;
        Context = context;
    }

    internal void Build(GameSceneConfigBuilder builder)
    {
        ConfigureRenderer(builder, builder.GraphicsDevice);
        ConfigureFeatures(builder);
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

    protected abstract void ConfigureFeatures(IGameSceneFeatureBuilder builder);
    protected abstract void ConfigureModules(IGameSceneModuleBuilder builder);
    protected abstract void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics);

    public abstract void Initialize();
    public abstract void Unload();
}