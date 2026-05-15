using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene.Modules;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;

    public ModuleManager Modules { get; }
    public SceneManager SceneManager { get; }

    public Terrain ActiveTerrain => Terrain.Main;
    public Skybox ActiveSkybox => Skybox.Instance;
    public ParticleSystemCore ParticleSystem => ParticleSystemCore.Instance;

    public T GetSystem<T>() where T : class, IGameEngineSystem => _systems.GetSystem<T>();

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
    public SceneManager SceneManager => _scene.SceneManager;

    public T GetSystem<T>() where T : class, IGameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}