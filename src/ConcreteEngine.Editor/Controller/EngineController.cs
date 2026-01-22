namespace ConcreteEngine.Editor.Controller;

public sealed class EngineController(
    WorldController worldController,
    InteractionController interactionController,
    SceneController sceneController,
    AssetController assetController)
{
    public readonly WorldController WorldController = worldController;
    public readonly InteractionController InteractionController = interactionController;
    public readonly SceneController SceneController = sceneController;
    public readonly AssetController AssetController = assetController;
}