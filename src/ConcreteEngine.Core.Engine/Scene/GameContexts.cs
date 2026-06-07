using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene.Modules;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class GameSceneContext
{
    public ModuleManager Modules { get; }
    public SceneStore SceneSpawner { get; }

    public Terrain ActiveTerrain => Terrain.Main;
    public Skybox ActiveSkybox => Skybox.Instance;

    internal GameSceneContext(ModuleManager modules, SceneStore sceneSpawner)
    {
        Modules = modules;
        SceneSpawner = sceneSpawner;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public SceneStore SceneSpawner => _scene.SceneSpawner;

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}