using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Windowing;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private EditorPortal _editor = null!;
    private InputController _inputController = null!;

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

        _inputController = new InputController(input);
        _editor = new EditorPortal(window, _inputController);
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

        SceneObjectProxyFactory.SceneStore = context.SceneManager.Store;
        SceneObjectProxyFactory.World = context.World;

        //var entityController = new EntityApiController(context);
        var engineController = new EngineController(
            new WorldApiController(context),
            new InteractionApiController(context),
            new SceneApiController(context),
            new AssetApiController(context));

        EditorSetup.RegisterCommands();
        EngineMetricHub.WireEditor();

        EngineCommandRouter.CommandCommandQueues = commandQueues;

        _editor.Initialize(engineController);
    }

    public void RenderEditor(float deltaTime, Size2D windowSize)
    {
        if (!Active) return;
        _inputController.Update();
        _editor.MainRender(deltaTime, windowSize);
    }


    public void UpdateDiagnostics(float delta)
    {
        if (!Active) return;
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