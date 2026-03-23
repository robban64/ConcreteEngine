using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext(AssetStore assetStore, SceneManager sceneManager)
{
    public readonly AssetStore AssetStore = assetStore;
    public readonly SceneManager SceneManager = sceneManager;
}