#region

using ConcreteEngine.Common.Diagnostics.Utility;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Core.Worlds.Render;
using ConcreteEngine.Core.Worlds.View;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Core.Scene;

//TODO rework
public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;

    protected IWorld World => Context.World;
    protected Camera3D Camera => World.Camera;


    protected GameScene()
    {
    }

    internal void Update(in UpdateTickInfo frameCtx, Size2D output)
    {
        Context.Modules.Update(in frameCtx);
    }

    internal void UpdateTick(int tick)
    {
        Context.Modules.GameTickUpdate(tick);
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