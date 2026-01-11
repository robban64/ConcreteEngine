using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Bridge;

public static class EngineController
{
    public static EngineWorldController WorldController = null!;
    public static EngineInteractionController InteractionController = null!;
    public static EngineSceneController SceneController = null!;
    public static EngineAssetController AssetController = null!;
}