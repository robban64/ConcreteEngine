using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.Diagnostics;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private const int DefaultLogDrain = 12;
    private static EditorPortal _editor = null!;
    private static LogParser _logParser = null!;

    private ApiContext _apiContext = null!;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; }

    private int _ticker, _slowTicker;

    internal EngineGateway(in EditorPortalArgs editorArgs)
    {
        if (_editor != null || _logParser != null)
            throw new InvalidOperationException("Debug Tools and Log Parsers is already active.");

        _editor = new EditorPortal(in editorArgs);
        _logParser = new LogParser();
    }

    public bool HasBindings => HasBoundEditor || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;
    public bool BlockInput() => Enabled && _editor.BlockInput();

    public static void ToggleEngineLogger(bool enabled) => Logger.Enabled = enabled;
    public static void ToggleGfxLogger(bool enabled) => GfxLog.Enabled = enabled;

    public static void SetupLogger()
    {
        Logger.Enabled = true;
        Logger.Attach(EditorSetup.ProcessStringLog);

        GfxLog.Enabled = true;
        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
    }

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

        _apiContext = context;
        var entityController = new EntityApiController(_apiContext);
        var worldController = new WorldApiController(_apiContext);
        var interactionController = new InteractionController(_apiContext);
        var sceneController = new SceneApiController(_apiContext);
        EditorSetup.Editor = _editor;
        EngineMetricRouter.Attach(context.World, context.AssetSystem);
        EngineResourceProvider.Attach(context.AssetSystem, entityController, worldController, sceneController);

        EngineController.EntityController = entityController;
        EngineController.InteractionController = interactionController;
        EngineController.WorldController = worldController;

        EditorSetup.RegisterDataProvider();
        EditorSetup.RegisterCommands();
        EditorSetup.RegisterMetrics();

        EngineCommandHandler.CommandQueues = editorQueues;

        _editor.Initialize();
    }


    public void RenderEditor(in RenderFrameInfo frameInfo, GfxFrameResult frameResult)
    {
        if (!Enabled || !HasBoundEditor) return;
        _editor.Render(frameInfo.DeltaTime);
    }

    public void UpdateDiagnostics(in RenderFrameInfo frameInfo, GfxFrameResult frameResult)
    {
        if (!Enabled) return;
        if (Logger.HasPendingStringLogs) Logger.FlushStringLogs();
        DrainLogs();

        if (_editor.IsMetricsMode) RefreshMetrics(frameInfo, frameResult);
    }

    private void RefreshMetrics(in RenderFrameInfo frameInfo, GfxFrameResult frameResult, in bool force = false)
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

        MetricsApi.FrameSample =
            new RenderInfoSample(frameInfo.Fps, frameInfo.Alpha, frameResult.DrawCalls, frameResult.TriangleCount);
        MetricsApi.FrameMetrics = new FrameMetric(frameInfo.FrameIndex, EngineTime.Timestamp, default);

        MetricsApi.RefreshFrameMetrics();

        switch (_ticker++)
        {
            case 0: MetricsApi.RefreshSceneMetrics(); break;
            case 5: MetricsApi.RefreshGfxResourceMetrics(); break;
            case >= 10:
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

    private static void DrainLogs()
    {
        var gfxLogLeft = DefaultLogDrain;
        var engineLogLeft = DefaultLogDrain;
        while (GfxLog.TryDrainLog(out var log) && gfxLogLeft-- > 0)
            _editor.AddLog(_logParser.Format(in log));

        while (Logger.TryDrainLog(out var log) && engineLogLeft-- > 0)
            _editor.AddLog(_logParser.Format(in log));
    }

    public void Dispose()
    {
        Enabled = false;
        _editor.Dispose();
    }

    private static class EditorSetup
    {
        public static EditorPortal Editor = null!;

        public static void ProcessStringLog(StringLogEvent log)
        {
            if (_logParser is null) return;
            Editor?.AddLog(_logParser.Format(log));
        }

        public static void RegisterCommands()
        {
            // Editor commands
            EditorCmd.RegisterEditorCmd<EditorShaderCommand>(CoreCmdNames.AssetShader, EditorCommandScope.Engine,
                EngineCommandHandler.OnAssetShaderCmd);

            EditorCmd.RegisterEditorCmd<EditorShadowCommand>(CoreCmdNames.WorldShadow, EditorCommandScope.Engine,
                EngineCommandHandler.OnWorldShadowCmd);

            // Console commands
            EditorCmd.RegisterConsoleCmd<EditorShaderCommand>(CoreCmdNames.AssetShader, string.Empty,
                CommandParser.ParseShaderRequest);

            EditorCmd.RegisterConsoleCmd<EditorShadowCommand>(CoreCmdNames.WorldShadow, string.Empty,
                CommandParser.ParseShadowRequest);

            // Misc
            EditorCmd.RegisterNoOpConsoleCmd("inspect-structs", string.Empty,
                EngineCommandHandler.OnStructSizesCmd);
        }

        public static void RegisterDataProvider()
        {
            EditorApi.LoadAssetResources = EngineResourceProvider.GetEditorAssets;
            EditorApi.FetchAssetFiles = EngineResourceProvider.GetEditorAssetFiles;

            EditorApi.LoadSceneObjects = EngineResourceProvider.GetSceneObjects;

            EditorApi.LoadEntityResources = EngineResourceProvider.CreateEntityList;

            EditorApi.LoadParticleResources = EngineResourceProvider.GetParticleResources;
            EditorApi.LoadAnimationResources = EngineResourceProvider.GetAnimationResources;
        }


        public static void RegisterMetrics()
        {
            MetricsApi.PullMaterialMetrics = EngineMetricRouter.GetMaterialMetrics;
            MetricsApi.PullSceneMetrics = EngineMetricRouter.GetSceneMetrics;
            MetricsApi.PullMemoryMetrics = EngineMetricRouter.GetMemoryMetrics;
            MetricsApi.FillAssetMetrics = EngineMetricRouter.DrainAssetStoreMetrics;
            MetricsApi.FillGfxStoreMetrics = EngineMetricRouter.DrainGfxStoreMetrics;
        }
    }
}