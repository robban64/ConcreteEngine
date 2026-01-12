using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;

    public ModuleManager Modules { get; }
    public SceneManager SceneManager { get; }

    public World World => GetSystem<World>();

    public T GetSystem<T>() where T : GameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems, ModuleManager modules, SceneManager sceneManager)
    {
        _systems = systems;
        Modules = modules;
        SceneManager = sceneManager;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public World World => _scene.World;
    public SceneManager SceneManager => _scene.SceneManager;

    public T GetSystem<T>() where T : GameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}