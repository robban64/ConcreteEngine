using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene.Modules;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class GameSceneContext
{
    public readonly SceneManager SceneManager;
    public ModuleManager Modules { get; }
    public SceneStore Store => SceneManager.Store;

    public Terrain ActiveTerrain => Terrain.Main;
    public Skybox ActiveSkybox => Skybox.Current;

    internal GameSceneContext(ModuleManager modules, SceneManager sceneManager)
    {
        Modules = modules;
        SceneManager = sceneManager;
    }
}

public sealed class GameModuleContext
{
    internal GameModuleContext(GameSceneContext scene) { }
}