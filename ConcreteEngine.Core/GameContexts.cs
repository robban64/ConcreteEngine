#region

#endregion

#region

using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Modules;

#endregion

namespace ConcreteEngine.Core;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;
    public ModuleManager Modules { get; internal set; }
    public IWorld World { get; internal set; } = null!;
    public Camera3D Camera { get; internal set; } = null!;

    public T GetSystem<T>() where T : IGameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems)
    {
        _systems = systems;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public Camera3D Camera => _scene.Camera;

    public IWorld World => _scene.World;

    public T GetSystem<T>() where T : IGameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}