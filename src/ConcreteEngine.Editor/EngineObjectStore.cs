using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Editor;


internal static class EngineObjectStore
{
    public static RayCaster RayCaster = null!;
    public static SceneStore SceneStore = null!;
    public static AssetFileRegistry FileRegistry = null!;
    public static AssetStore Assets = null!;

    public static Camera Camera = null!;
    public static VisualEnvironment Visuals = null!;

    public static void Create(EditorEngineBundle bundle)
    {
        RayCaster = bundle.RayCaster;
        SceneStore = bundle.SceneStore;
        Camera = bundle.Camera;
        Visuals = bundle.Visuals;
        FileRegistry = bundle.FileRegistry;
        Assets = bundle.Assets;
    }
}