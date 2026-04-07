using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Gateway.Diagnostics;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.Windowing;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EngineGateway : IDisposable
{
    private EditorPortal _editor = null!;
    private EditorInputController _editorInputController = null!;

    public readonly EngineMetricHub Metrics;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; }


    internal EngineGateway(EngineCoreSystem coreSystem)
    {
        var scene = coreSystem.GetSystem<SceneSystem>();
        var asset = coreSystem.GetSystem<AssetSystem>();
        Metrics = new EngineMetricHub(scene.SceneManager, asset.Store);
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

    public void SetupEditorGateway(EngineCoreSystem coreSystem, EngineCommandQueue commandQueues)
    {
        ArgumentNullException.ThrowIfNull(commandQueues);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (HasBoundEditor) throw new InvalidOperationException(nameof(HasBoundEditor));
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));

        Enabled = true;
        HasBoundEditor = true;
        HasBoundMetrics = true;

        var sceneManager = coreSystem.GetSystem<SceneSystem>().SceneManager;
        var engineController = new EngineController(
            CameraManager.Instance.Camera,
            VisualManager.Instance.VisualEnv,
            new InteractionApiController(sceneManager),
            new SceneApiController(sceneManager),
            coreSystem.AssetSystem.AssetProvider);

        EditorSetup.RegisterCommands();

        Metrics.ConnectEditor(_editor.GetMetricSystem());

        EngineCommandRouter.CommandCommandQueues = commandQueues;

        _editor.Initialize(engineController);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame()
    {
        if (!Active) return;
        _editorInputController.Update();
        _editor.UpdateInput();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderEditor(float deltaTime, Size2D windowSize)
    {
        if (!Active) return;
        _editor.Render(deltaTime, windowSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateGameTick(float deltaTime)
    {
        _editor.UpdateGameTick(deltaTime);
    }

    public void UpdateDiagnostics(float delta)
    {
        if (!Active) return;
        Metrics.OnDiagnosticTick();
        _editor.UpdateDiagnostic();
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