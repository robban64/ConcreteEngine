using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;
    
    public ModuleManager Modules { get; }

    public World World { get; }

    public T GetSystem<T>() where T : IGameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems, World world, ModuleManager modules)
    {
        _systems = systems;
        World = world;
        Modules = modules;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public World World => _scene.World;
    public T GetSystem<T>() where T : IGameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}