using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext(World world, AssetStore assetStore, SceneManager sceneManager)
{
    public readonly World World = world;
    public readonly AssetStore AssetStore = assetStore;
    public readonly SceneManager SceneManager = sceneManager;
}