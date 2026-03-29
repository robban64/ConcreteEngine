using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext(AssetSystem assetSystem, SceneManager sceneManager)
{
    public readonly AssetSystem AssetSystem = assetSystem;
    public readonly SceneManager SceneManager = sceneManager;
}