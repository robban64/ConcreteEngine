using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Editor;

public sealed class EditorEngineContext
{
    public required GfxResourceApi GfxApi;
    public required InputController Input;
    public required Action<ViewportRect> OnViewportChanged;
}

public sealed class EditorEngineBundle
{
    public required Camera Camera;
    public required VisualEnvironment Visuals;
    public required InteractionController InteractionController;
    public required SceneController SceneController;
    public required AssetStore Assets;
    public required AssetFileRegistry FileRegistry;
}