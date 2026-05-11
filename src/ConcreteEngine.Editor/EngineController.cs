using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Editor;

public sealed class EditorEngineContext
{
    public required GfxResourceApi GfxApi;
    public required InputController Input;
    public required EngineWindow Window;
}

public sealed class EditorEngineBundle
{
    public required Camera Camera;
    public required VisualEnvironment Visuals;
    public required RayCaster RayCaster;
    public required SceneStore SceneStore;
    public required AssetStore Assets;
    public required AssetFileRegistry FileRegistry;
}