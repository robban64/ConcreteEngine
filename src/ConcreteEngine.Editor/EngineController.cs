using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor;

public sealed class EngineController(
    Camera camera,
    VisualEnvironment visuals,
    InteractionController interactionController,
    SceneController sceneController,
    AssetProvider assetProvider)
{
    public readonly Camera Camera = camera;
    public readonly VisualEnvironment Visuals = visuals;
    public readonly InteractionController InteractionController = interactionController;
    public readonly SceneController SceneController = sceneController;
    public readonly AssetProvider AssetProvider = assetProvider;
}