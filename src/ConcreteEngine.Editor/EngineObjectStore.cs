using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor;

internal static class EngineObjectStore
{
    public static SceneStore SceneStore = null!;
    public static SceneSpawner SceneSpawner = null!;
    public static AssetFileRegistry FileRegistry = null!;
    public static AssetStore Assets = null!;

    public static void Create(EditorEngineBundle bundle)
    {
        SceneStore = bundle.SceneStore;
        SceneSpawner = bundle.SceneSpawner;
        FileRegistry = bundle.FileRegistry;
        Assets = bundle.Assets;
    }
}