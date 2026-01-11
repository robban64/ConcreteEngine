using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Windowing;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private EditorPortal _editor = null!;
    private EditorController _editorController = null!;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; }

    internal EngineGateway()
    {
    }

    public bool HasBindings => HasBoundEditor || HasBoundMetrics;

    public bool Active
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Enabled && HasBindings;
    }
    
    public void OnResized() => _editor.OnResized();

    public void SetupEditor(IWindow window, InputSystem input)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(input);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (HasBoundEditor) throw new InvalidOperationException(nameof(HasBoundEditor));

        if (_editor != null)
            throw new InvalidOperationException("Debug Tools and Log Parsers is already active.");

        _editorController = new EditorController(input);
        _editor = new EditorPortal(window, _editorController);
    }

    public void SetupEditorGateway(EngineCommandQueue commandQueues, ApiContext context)
    {
        ArgumentNullException.ThrowIfNull(commandQueues);
        ArgumentNullException.ThrowIfNull(context);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (HasBoundEditor) throw new InvalidOperationException(nameof(HasBoundEditor));
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));

        Enabled = true;
        HasBoundEditor = true;
        HasBoundMetrics = true;

        //var entityController = new EntityApiController(context);
        var worldController = new WorldApiController(context);
        var interactionController = new InteractionApiController(context);
        var sceneController = new SceneApiController(context);
        var assetController = new AssetApiController(context);

        ProxyPropertyFactory.SceneStore = context.SceneManager.Store;
        ProxyPropertyFactory.World = context.World;

        //EngineController.EntityController = entityController;
        EngineController.InteractionController = interactionController;
        EngineController.WorldController = worldController;
        EngineController.SceneController = sceneController;
        EngineController.AssetController = assetController;

        EditorSetup.RegisterCommands();
        EngineMetricHub.WireEditor();

        EngineCommandRouter.CommandCommandQueues = commandQueues;

        _editor.Initialize();
    }

    public void RenderEditor(float deltaTime, Size2D windowSize)
    {
        if (!HasBindings) return;
        _editorController.Update();
        _editor.MainRender(deltaTime, windowSize);
    }


    public void UpdateDiagnostics(float delta)
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
            EditorCmd.RegisterCommand<AssetCommandRecord>(EngineCommandRouter.AssetEndpoint);
            EditorCmd.RegisterCommand<FboCommandRecord>(EngineCommandRouter.RenderEndpoint);

            // Console commands
            EditorCmd.RegisterConsoleCmd(CliName.Asset, string.Empty, CommandParser.ParseAssetRequest);
            EditorCmd.RegisterConsoleCmd(CliName.Graphics, string.Empty, CommandParser.ParseShadowRequest);

            // Misc
            EditorCmd.RegisterNoOpConsoleCmd("inspect-structs", string.Empty, DebugCommandRouter.OnStructSizesCmd);
        }
    }
}