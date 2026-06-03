using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor;

internal static class EngineObjectStore
{
    public static SceneStore SceneStore = null!;

    public static void Create(EditorEngineBundle bundle)
    {
        SceneStore = bundle.SceneStore;
    }
}