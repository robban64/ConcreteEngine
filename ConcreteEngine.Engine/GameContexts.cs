using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;
    
    public ModuleManager Modules { get; }
    public World World { get; }
    public SceneWorld SceneWorld { get; }

    public T GetSystem<T>() where T : class, IGameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems, World world, ModuleManager modules, SceneWorld sceneWorld)
    {
        _systems = systems;
        World = world;
        Modules = modules;
        SceneWorld = sceneWorld;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public World World => _scene.World;
    public SceneWorld SceneWorld => _scene.SceneWorld;

    public T GetSystem<T>() where T : class, IGameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}