namespace ConcreteEngine.Editor.Controller;

public sealed class EngineController(
    InteractionController interactionController,
    SceneController sceneController,
    AssetController assetController)
{
    public readonly InteractionController InteractionController = interactionController;
    public readonly SceneController SceneController = sceneController;
    public readonly AssetController AssetController = assetController;
}