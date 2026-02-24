using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Editor.Bridge;

public sealed class EngineController(
    EngineCamera camera,
    VisualEnvironment visuals,
    InteractionController interactionController,
    SceneController sceneController,
    AssetController assetController)
{
    public readonly EngineCamera Camera = camera;
    public readonly VisualEnvironment Visuals = visuals;
    public readonly InteractionController InteractionController = interactionController;
    public readonly SceneController SceneController = sceneController;
    public readonly AssetController AssetController = assetController;
}