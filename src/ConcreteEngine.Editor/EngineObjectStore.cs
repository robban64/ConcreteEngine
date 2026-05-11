using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor;


internal static class EngineObjectStore
{
    public static InteractionController InteractionController = null!;
    public static SceneController SceneController = null!;
    public static AssetFileRegistry FileRegistry = null!;
    public static AssetStore Assets = null!;

    public static Camera Camera = null!;
    public static VisualEnvironment Visuals = null!;

    public static void Create(EditorEngineBundle bundle)
    {
        InteractionController = bundle.InteractionController;
        SceneController = bundle.SceneController;
        Camera = bundle.Camera;
        Visuals = bundle.Visuals;
        FileRegistry = bundle.FileRegistry;
        Assets = bundle.Assets;
    }
}