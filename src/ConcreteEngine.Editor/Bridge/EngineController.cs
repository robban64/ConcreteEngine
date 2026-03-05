using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor.Bridge;

public sealed class EngineController(
    CameraTransform camera,
    VisualEnvironment visuals,
    InteractionController interactionController,
    SceneController sceneController,
    AssetController assetController)
{
    public readonly CameraTransform Camera = camera;
    public readonly VisualEnvironment Visuals = visuals;
    public readonly InteractionController InteractionController = interactionController;
    public readonly SceneController SceneController = sceneController;
    public readonly AssetController AssetController = assetController;
}