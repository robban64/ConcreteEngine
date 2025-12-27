using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Metadata.Command;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private static EditorPortal _editor = null!;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; }

    internal EngineGateway(in EditorPortalArgs editorArgs)
    {
        if (_editor != null)
            throw new InvalidOperationException("Debug Tools and Log Parsers is already active.");

        _editor = new EditorPortal(in editorArgs);
    }

    public bool HasBindings => HasBoundEditor || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;
    public bool BlockInput() => Enabled && _editor.BlockInput;


    public void SetupEditor(EditorEngineQueue editorQueues, ApiContext context)
    {
        ArgumentNullException.ThrowIfNull(editorQueues);
        ArgumentNullException.ThrowIfNull(context);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (HasBoundEditor) throw new InvalidOperationException(nameof(HasBoundEditor));
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));

        Enabled = true;
        HasBoundEditor = true;
        HasBoundMetrics = true;

        var entityController = new EntityApiController(context);
        var worldController = new WorldApiController(context);
        var interactionController = new InteractionApiController(context);
        var sceneController = new SceneApiController(context);
        var assetController = new AssetApiController(context);

        EngineController.EntityController = entityController;
        EngineController.InteractionController = interactionController;
        EngineController.WorldController = worldController;
        EngineController.SceneController = sceneController;
        EngineController.AssetController = assetController;

        EditorSetup.RegisterCommands();
        EngineMetricHub.WireEditor();

        EngineCommandRouter.CommandQueues = editorQueues;

        _editor.Initialize();
    }

    public void RenderEditor(float delta)
    {
        if (!HasBindings) return;
        _editor.Render(delta);
    }


    public void UpdateDiagnostics()
    {
        if (!Enabled) return;
        _editor.OnTickDiagnostic();
    }

    public void Dispose()
    {
        Enabled = false;
        _editor.Dispose();
    }

    private static class EditorSetup
    {
        public static void RegisterCommands()
        {
            // Editor commands
            EditorCmd.RegisterCommand<AssetCommandRecord>(EngineCommandRouter.OnAssetShaderCmd);
            EditorCmd.RegisterCommand<FboCommandRecord>(EngineCommandRouter.OnWorldShadowCmd);

            // Console commands
            EditorCmd.RegisterConsoleCmd(CliName.AssetShader, string.Empty, CommandParser.ParseAssetRequest);
            EditorCmd.RegisterConsoleCmd(CliName.WorldShadow, string.Empty, CommandParser.ParseShadowRequest);

            // Misc
            EditorCmd.RegisterNoOpConsoleCmd("inspect-structs", string.Empty, DebugCommandRouter.OnStructSizesCmd);
        }
    }
}