using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor.Bridge;

internal static class EngineObjectStore
{
    public static InteractionController InteractionController = null!;
    public static SceneController SceneController = null!;
    public static AssetController AssetController = null!;

    public static Camera Camera = null!;
    public static VisualEnvironment Visuals = null!;

    public static void Init(EngineController controller)
    {
        InteractionController = controller.InteractionController;
        SceneController = controller.SceneController;
        AssetController = controller.AssetController;
        Camera = controller.Camera;
        Visuals = controller.Visuals;
    }
}