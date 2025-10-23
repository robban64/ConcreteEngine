#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Core.Scene;

public abstract class GameScene
{
    private World _world = null!;
    private readonly Camera3D _camera;

    protected GameSceneContext Context { get; private set; } = null!;

    protected World World => _world;
    protected Camera3D Camera => _camera;

    internal World InternalWorld => _world;

    protected GameScene()
    {
        _camera = new Camera3D();
    }

    internal void Update(in UpdateTickInfo frameCtx, Size2D output)
    {
        _camera.Viewport = output;
        Context.Modules.Update(in frameCtx);
    }

    internal void UpdateTick(int tick)
    {
        Context.Modules.GameTickUpdate(tick);
        World.Cleanup();
    }

    internal void BeforeRender(out RenderViewSnapshot viewSnapshot)
    {
        _camera.MakeRenderViewInfo(out viewSnapshot);
    }


    internal void AttachContext(GameSceneContext context)
    {
        var renderer = context.GetSystem<IRenderingSystem>();
        _world = new World(renderer.SceneProperties, renderer.Batchers);
        context.World = World;
        context.Camera = _camera;
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