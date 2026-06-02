using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor;

public sealed class EditorEngineContext
{
    public required InputController Input;
    public required EngineWindow Window;
}

public sealed class EditorEngineBundle
{
    public required SceneStore SceneStore;
    public required SceneSpawner SceneSpawner;
}