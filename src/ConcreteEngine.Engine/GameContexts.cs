using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Scene.Modules;

namespace ConcreteEngine.Engine;

public sealed class GameSceneContext
{
    private readonly IEngineSystemManager _systems;

    public ModuleManager Modules { get; }
    public SceneManager SceneManager { get; }

    public Terrain ActiveTerrain => TerrainManager.Instance.Terrain;
    public Skybox ActiveSkybox => Skybox.Instance;
    public ParticleManager ParticleManager => ParticleManager.Instance;

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
    public SceneManager SceneManager => _scene.SceneManager;

    public T GetSystem<T>() where T : GameEngineSystem => _scene.GetSystem<T>();

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}