using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EngineGateway : IDisposable
{
    public bool Enabled { get; private set; }

    public readonly EngineMetricHub Metrics;

    private readonly RenderProgram _renderProgram;
    private EditorPortal _editor = null!;

    internal EngineGateway(RenderProgram renderProgram)
    {
        _renderProgram = renderProgram;
        Metrics = new EngineMetricHub();
    }

    public void SetupEditor(EngineCommandQueue commandQueue, GfxContext gfxContext)
    {
        ArgumentNullException.ThrowIfNull(commandQueue);
        ArgumentNullException.ThrowIfNull(gfxContext);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (_editor != null) throw new InvalidOperationException("Editor is already setup.");

        Enabled = true;

        _editor = new EditorPortal();
        Metrics.ConnectEditor(_editor.GetMetricSystem());

        EditorSetup.RegisterCommands();
        EngineCommandRouter.CommandCommandQueues = commandQueue;

        _editor.Start();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame()
    {
        if (!Enabled) return;
        _editor.UpdateInput();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderEditor(float deltaTime)
    {
        if (!Enabled) return;
        _editor.Render(deltaTime, _renderProgram.OutputTexture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateGameTick(float deltaTime)
    {
        if (!Enabled) return;
        _editor.UpdateGameTick(deltaTime);
    }

    public void UpdateDiagnostics(float delta)
    {
        if (!Enabled) return;
        Metrics.OnDiagnosticTick();
        _editor.OnDiagnosticTick();
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
            EditorCmd.RegisterConsoleCmd(CliName.Asset, string.Empty,
                static (action, arg1, arg2) => CommandParser.ParseAssetRequest(action, arg1, arg2));

            EditorCmd.RegisterConsoleCmd(CliName.Graphics, string.Empty,
                static (action, arg1, arg2) => CommandParser.ParseShadowRequest(action, arg1, arg2));

            // Misc
            EditorCmd.RegisterNoOpConsoleCmd("inspect-structs", string.Empty, DebugCommandRouter.OnStructSizesCmd);
        }
    }
}