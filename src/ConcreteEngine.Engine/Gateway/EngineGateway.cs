using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EngineGateway : IDisposable
{
    public bool Enabled { get; private set; }

    public readonly EngineMetricHub Metrics;

    private readonly EngineWindow _window;
    private readonly RenderProgram _renderProgram;
    private EditorPortal _editor = null!;

    internal EngineGateway(EngineWindow window, EngineCoreSystem coreSystem)
    {
        var scene = coreSystem.GetSystem<SceneSystem>();
        var asset = coreSystem.GetSystem<AssetSystem>();
        _renderProgram = coreSystem.GetSystem<EngineRenderSystem>().Program;
        _window = window;
        Metrics = new EngineMetricHub(scene.SceneManager, asset.Store);
    }

    public void SetupEditor(EngineCoreSystem coreSystem, EngineWindow window, EngineCommandQueue commandQueues,
        GfxContext gfxContext)
    {
        ArgumentNullException.ThrowIfNull(coreSystem);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(gfxContext);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (_editor != null) throw new InvalidOperationException("Editor is already setup.");

        Enabled = true;

        var sceneManager = coreSystem.GetSystem<SceneSystem>().SceneManager;
        var inputSystem = coreSystem.GetSystem<InputSystem>();

        var engineBundle = new EditorEngineBundle
        {
            Camera = CameraSystem.Instance.Camera,
            Visuals = VisualManager.Instance,
            RayCaster = CameraSystem.Instance.RayCaster,
            SceneStore = sceneManager.Store,
            Assets = coreSystem.AssetSystem.Store,
            FileRegistry = coreSystem.AssetSystem.FileRegistry,
        };

        var engineContext = new EditorEngineContext
        {
            GfxApi = gfxContext.ResourceManager.GetGfxApi(),
            Input = new InputLayerController(inputSystem, InputLayerKind.Ui),
            Window =  window,

        };

        _editor = new EditorPortal( engineContext, engineBundle);
        Metrics.ConnectEditor(_editor.GetMetricSystem());

        EditorSetup.RegisterCommands();
        EngineCommandRouter.CommandCommandQueues = commandQueues;

        _editor.Start(_window.OutputSize);
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
        _editor.Render(deltaTime, _window.OutputSize, _renderProgram.OutputTexture);
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