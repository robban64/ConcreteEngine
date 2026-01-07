using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;

    public ModuleManager Modules { get; }
    public Scene.Scene Scene { get; }

    public World World => GetSystem<World>();

    public T GetSystem<T>() where T : GameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems, ModuleManager modules, Scene.Scene scene)
    {
        _systems = systems;
        Modules = modules;
        Scene = scene;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public World World => _scene.World;
    public Scene.Scene Scene => _scene.Scene;

    public T GetSystem<T>() where T : GameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}