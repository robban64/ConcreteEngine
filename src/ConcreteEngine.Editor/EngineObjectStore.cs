using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor;

internal static class EngineObjectStore
{
    public static InteractionController InteractionController = null!;
    public static SceneController SceneController = null!;
    public static AssetProvider AssetProvider = null!;

    public static Camera Camera = null!;
    public static VisualEnvironment Visuals = null!;

    public static void Create(EngineController controller)
    {
        InteractionController = controller.InteractionController;
        SceneController = controller.SceneController;
        AssetProvider = controller.AssetProvider;
        Camera = controller.Camera;
        Visuals = controller.Visuals;
    }
}