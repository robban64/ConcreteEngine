#region

#endregion

#region

using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;

#endregion

namespace ConcreteEngine.Engine;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;
    public ModuleManager Modules { get; internal set; }
    public IWorld World { get; }

    public T GetSystem<T>() where T : IGameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems, IWorld world)
    {
        _systems = systems;
        World = world;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public IWorld World => _scene.World;
    public T GetSystem<T>() where T : IGameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}