using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.Diagnostics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private const int DefaultLogDrain = 12;
    private static EditorPortal _editor = null!;
    private static LogParser _logParser = null!;

    private ApiContext _apiContext = null!;
    private EntityApiController _entityController = null!;
    private WorldApiController _worldController = null!;
    private InteractionController _interactionController = null!;

    public bool HasBoundEditor { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; }

    private int _ticker, _slowTicker;


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
        if (Logger.Enabled) Logger.Attach(EditorSetup.ProcessStringLog);

        GfxLog.ToggleLog(false, LogTopic.Unknown, LogScope.Backend);
        GfxLog.ToggleLog(false, LogTopic.RenderBuffer, LogScope.Gfx);
    }

    public void SetupEditor(EditorEngineQueue editorQueues, World world, AssetSystem assetSystem)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(assetSystem);

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
        MetricRouter.Attach(world, assetSystem);
        EngineResourceProvider.Attach(assetSystem, _entityController, _interactionController, _worldController);

        EngineController.EntityController = _entityController;
        EngineController.InteractionController = _interactionController;
        EngineController.WorldController = _worldController;

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
        DrainLogs();

        if (_editor.IsMetricsMode)
            RefreshMetrics(frameInfo, frameResult);
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

        public static void ProcessStringLog(StringLogEvent log) => Editor.AddLog(_logParser.Format(log));

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
            EditorApi.FetchAssetDetailed = EngineResourceProvider.GetAssetObjectFiles;
            EditorApi.LoadAssetResources = EngineResourceProvider.CreateEditorAssets;
            EditorApi.LoadEntityResources = EngineResourceProvider.CreateEntityList;

            EditorApi.LoadParticleResources = EngineResourceProvider.GetParticleResources;
            EditorApi.LoadAnimationResources = EngineResourceProvider.GetAnimationResources;
        }


        public static void RegisterMetrics()
        {
            MetricsApi.PullMaterialMetrics = MetricRouter.GetMaterialMetrics;
            MetricsApi.PullSceneMetrics = MetricRouter.GetSceneMetrics;
            MetricsApi.PullMemoryMetrics = MetricRouter.GetMemoryMetrics;
            MetricsApi.FillAssetMetrics = MetricRouter.DrainAssetStoreMetrics;
            MetricsApi.FillGfxStoreMetrics = MetricRouter.DrainGfxStoreMetrics;
        }
    }
}