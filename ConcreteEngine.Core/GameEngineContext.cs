#region

#endregion

using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Systems;

namespace ConcreteEngine.Core;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;

    public required FeatureManager Features { get; init; }
    public required ModuleManager Modules { get; init; }
    public IWorld World { get; internal set; } = null!;
    public T GetSystem<T>() where T : IGameEngineSystem => _systems.GetSystem<T>();

    internal GameSceneContext(IEngineSystemManager systems)
    {
        _systems = systems;
    }
}

public sealed class GameFeatureContext
{
    private readonly GameSceneContext _scene;

    public FeatureManager Features => _scene.Features;
    public ModuleManager Modules => _scene.Modules;
    public IWorld World => _scene.World;
    

    public T GetSystem<T>() where T : IGameEngineSystem => _scene.GetSystem<T>();


    internal GameFeatureContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public FeatureManager Features => _scene.Features;
    public ModuleManager Modules => _scene.Modules;

    public IWorld World => _scene.World;

    public T GetSystem<T>() where T : IGameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}