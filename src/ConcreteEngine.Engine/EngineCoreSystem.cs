using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine;

public sealed class EngineCoreSystem : IEngineSystemManager, IDisposable
{
    internal readonly AssetSystem Assets;
    internal readonly SceneSystem Scene;
    internal readonly EngineRenderSystem Render;
    
    internal readonly EngineCommandQueue CommandQueues;

    private FrameStepper _systemStepper = new(8);

    private readonly Dictionary<Type, IGameEngineSystem> _systems = new(4);

    internal EngineCoreSystem(GraphicsRuntime graphics, List<Func<GameScene>> sceneFactories)
    {
        Assets = new AssetSystem(graphics.Gfx);
        Render = new EngineRenderSystem(graphics);
        Scene = new SceneSystem(sceneFactories);

        CommandQueues = new EngineCommandQueue(new EngineCommandContext(Assets));

        Register(Assets);
        Register(Scene);
        Register(Render);
    }
    
    
    internal void OnSystemTick(float dt)
    {
        if (_systemStepper.Tick())
        {
            var windowResized = EngineWindow.Commit();
            Render.OnSystemTick(windowResized);
        }

        if (Assets.PendingAssetCount > 0)
            Assets.ProcessPendingQueue();

        if (CommandQueues.QueuesCount > 0)
            CommandQueues.DrainDispatch();
        
        TerrainSystem.Instance.OnTick();
    }


    private void Register<T>(T system) where T : class, IGameEngineSystem
    {
        if (!_systems.TryAdd(typeof(T), system))
            throw new InvalidOperationException($"System of type {typeof(T)} is already registered");
    }

    public T GetSystem<T>() where T : class, IGameEngineSystem
    {
        if (!_systems.TryGetValue(typeof(T), out var system) || system is not T t)
            throw new InvalidOperationException($"System  of type {typeof(T)} is not registered or wrong type");

        return t;
    }

    public void Dispose()
    {
        Scene.Shutdown();
        Render.Shutdown();
        Assets.Shutdown();
    }
}