using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.Windowing;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private EditorPortal _editor = null!;
    private EditorInputController _editorInputController = null!;

    private readonly EngineMetricHub _metrics;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; }

    internal EngineGateway(EngineMetricHub metrics)
    {
        _metrics = metrics;
    }

    public bool HasBindings => HasBoundEditor || HasBoundMetrics;

    public bool Active
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Enabled && HasBindings;
    }

    public void OnResized() => _editor.OnResized();

    public void SetupEditor(IWindow window, InputSystem input, GfxContext gfxContext)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(input);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (HasBoundEditor) throw new InvalidOperationException(nameof(HasBoundEditor));

        if (_editor != null)
            throw new InvalidOperationException("Debug Tools and Log Parsers is already active.");

        InspectorBinder.RegisterTypes();
        _editorInputController = new EditorInputController(input);
        _editor = new EditorPortal(window, _editorInputController, gfxContext);
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

        var engineController = new EngineController(
            CameraSystem.Instance.Camera,
            VisualSystem.Instance.VisualEnv,
            new InteractionApiController(context),
            new SceneApiController(context),
            new AssetApiController(context));

        EditorSetup.RegisterCommands();

        _metrics.ConnectEditor(_editor.GetMetricSystem());

        EngineCommandRouter.CommandCommandQueues = commandQueues;

        _editor.Initialize(engineController);
    }

    public void BeginFrame()
    {
        if (!Active) return;
        _editorInputController.Update();
        _editor.UpdateInput();
    }

    public void RenderEditor(float deltaTime, Size2D windowSize)
    {
        if (!Active) return;
        //_editorInputController.Update();
        _editor.Render(deltaTime, windowSize);
    }


    public void UpdateDiagnostics(float delta)
    {
        if (!Active) return;
        _metrics.OnDiagnosticTick();
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
            EditorCmd.RegisterCommand<AssetCommandRecord>(static (record, meta) =>
                EngineCommandRouter.AssetEndpoint(record, meta));
            EditorCmd.RegisterCommand<FboCommandRecord>(static (record, meta) =>
                EngineCommandRouter.RenderEndpoint(record, meta));

            // Console commands
            EditorCmd.RegisterConsoleCmd(CliName.Asset, string.Empty,
                static (action, arg1, arg2) => CommandParser.ParseAssetRequest(action, arg1, arg2));

            EditorCmd.RegisterConsoleCmd(CliName.Graphics, string.Empty,
                static (action, arg1, arg2) => CommandParser.ParseShadowRequest(action, arg1, arg2));

            // Misc
            EditorCmd.RegisterNoOpConsoleCmd("inspect-structs", string.Empty, DebugCommandRouter.OnStructSizesCmd);
        }
    }
}