#region

#region

#region

#region

using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Shared.Diagnostics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Renderer.State;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

#endregion

#endregion

#endregion

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private static EditorPortal _editor = null!;
    private static LogParser _logParser = null!;

    private ApiContext _apiContext = null!;
    private EntityApiController _entityController = null!;
    private WorldApiController _worldController = null!;
    private InteractionController _interactionController = null!;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; } = false;

    private int _ticker = 0, _mediumTicker = 0, _slowTicker = 0;

    private bool _drainGfxLogs;

    internal EngineGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        if (_editor != null || _logParser != null)
            throw new InvalidOperationException("Debug Tools and Log Parsers is already active.");

        _editor = new EditorPortal(gl, window, inputCtx);
        _logParser = new LogParser();
    }

    public bool HasBindings => HasBoundEditor || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;
    public bool BlockInput() => Enabled && _editor.BlockInput();
    public static void ToggleEngineLogger(bool enabled) => Logger.Enabled = enabled;
    public static void ToggleGfxLogger(bool enabled) => GfxLog.Enabled = enabled;

    public static void SetupLogger()
    {
        if (Logger.Enabled && !Logger.IsAttached) Logger.Attach(EditorSetup.ProcessStringLog);

        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
    }

    public void SetupEditor(EditorEngineQueue editorQueues, World world, AssetSystem assetSystem,
        RenderEngineFrameInfo frameInfo)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(assetSystem);
        ArgumentNullException.ThrowIfNull(frameInfo);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (HasBoundEditor) throw new InvalidOperationException(nameof(HasBoundEditor));
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));

        Enabled = true;
        HasBoundEditor = true;
        HasBoundMetrics = true;

        _apiContext = new ApiContext(world, assetSystem);
        _entityController = new EntityApiController(_apiContext);
        _worldController = new WorldApiController(_apiContext);
        _interactionController = new InteractionController(_apiContext);

        EditorSetup.Editor = _editor!;
        MetricRouter.Attach(world, assetSystem, frameInfo);
        EngineResourceProvider.Attach(assetSystem, _entityController, _interactionController);
        EngineDataBridge.Attach(_entityController, _worldController, _interactionController);

        EditorSetup.RegisterDataProvider();
        EditorSetup.RegisterCommands();
        EditorSetup.RegisterMetrics();

        EngineCommandHandler.CommandQueues = editorQueues;

        _editor.Initialize();
    }


    public void RenderEditor(in RenderFrameInfo frameInfo)
    {
        if (!Enabled || !HasBoundEditor) return;
        EngineDataBridge.ProcessEditorDataSlot();
        _apiContext.OnRenderFrame(in frameInfo);
        _editor.Render(frameInfo.DeltaTime);
    }

    public void UpdateDiagnostics(UpdateTickArgs args)
    {
        if (!Enabled) return;
        DrainLogs();
        RefreshMetrics();
    }

    private void RefreshMetrics(bool force = false)
    {
        if (force)
        {
            MetricsApi.RefreshFrameMetrics();
            MetricsApi.RefreshAssetMetrics();
            MetricsApi.RefreshGfxResourceMetrics();
            MetricsApi.RefreshSceneMetrics();
            MetricsApi.RefreshMemoryMetrics();
            return;
        }

        MetricsApi.RefreshFrameMetrics();

        switch (_ticker++)
        {
            case 0: MetricsApi.RefreshSceneMetrics(); break;
            case 4: MetricsApi.RefreshGfxResourceMetrics(); break;
            case >= 8:
                MetricsApi.RefreshAssetMetrics();
                _ticker = 0;
                break;
        }
        
        if (_slowTicker++ >= 16)
        {
            _slowTicker = 0;
            MetricsApi.RefreshMemoryMetrics();
        }
    }

    private void DrainLogs()
    {
        if (_drainGfxLogs && GfxLog.Count > 0)
        {
            var logs = GfxLog.DrainLogs();
            foreach (ref readonly var log in logs)
                _editor.AddLog(_logParser.Format(in log));
        }

        if (!_drainGfxLogs && Logger.Count > 0)
        {
            var logs = Logger.DrainLogs();
            foreach (ref readonly var log in logs)
                _editor.AddLog(_logParser.Format(in log));
        }

        _drainGfxLogs = !_drainGfxLogs;
    }

    public void Dispose()
    {
        Enabled = false;
        _editor.Dispose();
    }

    private static class EditorSetup
    {
        public static EditorPortal Editor = null!;

        public static void ProcessStringLog(StringLogEvent log) => Editor.AddLog(_logParser.Format(log));

        public static void RegisterCommands()
        {
            // Editor commands
            EditorCmd.RegisterEditorCmd<EditorShaderPayload>(CoreCmdNames.AssetShader, EditorCommandScope.Engine,
                EngineCommandHandler.OnAssetShaderCmd);

            EditorCmd.RegisterEditorCmd<EditorShadowPayload>(CoreCmdNames.WorldShadow, EditorCommandScope.Engine,
                EngineCommandHandler.OnWorldShadowCmd);

            // Console commands
            EditorCmd.RegisterConsoleCmd<EditorShaderPayload>(CoreCmdNames.AssetShader, string.Empty,
                CommandParser.ParseShaderRequest);

            EditorCmd.RegisterConsoleCmd<EditorShadowPayload>(CoreCmdNames.WorldShadow, string.Empty,
                CommandParser.ParseShadowRequest);

            // Misc
            EditorCmd.RegisterNoOpConsoleCmd("inspect-structs", string.Empty,
                EngineCommandHandler.OnStructSizesCmd);
        }

        public static void RegisterDataProvider()
        {
            EditorApi.FetchAssetDetailed = EngineResourceProvider.GetAssetObjectFiles;

            EditorApi.LoadAssetResources = EngineResourceProvider.CreateEditorAssets;
            EditorApi.LoadEntityResources = EngineResourceProvider.CreateEntityList;

        }


        public static unsafe void RegisterMetrics()
        {
            MetricsApi.PullFrameMetrics = &MetricRouter.GetFrameMetrics;
            MetricsApi.PullMaterialMetrics = MetricRouter.GetMaterialMetrics;
            MetricsApi.PullSceneMetrics = MetricRouter.GetSceneMetrics;
            MetricsApi.PullMemoryMetrics = MetricRouter.GetMemoryMetrics;
            MetricsApi.FillAssetMetrics = MetricRouter.DrainAssetStoreMetrics;
            MetricsApi.FillGfxStoreMetrics = MetricRouter.DrainGfxStoreMetrics;
        }
    }
}