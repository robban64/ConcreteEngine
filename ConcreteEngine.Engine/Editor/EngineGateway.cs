#region

using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
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

using EditorCmd = ConcreteEngine.Editor.CommandDispatcher;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EngineGateway : IDisposable
{
    private static EditorPortal _debugTools = null!;
    private static LogParser _logParser = null!;

    public bool HasBoundCommands { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; } = true;

    private int _ticker = 0, _mediumTicker = 0, _slowTicker = 0;

    private bool _drainGfxLogs;

    internal EngineGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        if (_debugTools != null || _logParser != null)
            throw new InvalidOperationException("Debug Tools and Log Parsers is already active.");

        _debugTools = new EditorPortal(gl, window, inputCtx);
        _logParser = new LogParser();
    }

    public bool HasBindings => HasBoundCommands || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;
    public bool BlockInput() => Enabled && _debugTools.BlockInput();
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
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(assetSystem, nameof(assetSystem));
        ArgumentNullException.ThrowIfNull(frameInfo, nameof(frameInfo));

        if (!Enabled) return;
        if (HasBoundCommands) throw new InvalidOperationException(nameof(HasBoundCommands));
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));
        HasBoundCommands = true;
        HasBoundMetrics = true;

        EditorSetup.DebugTools = _debugTools!;
        EditorSetup.AttachEditor(world, assetSystem, frameInfo);
        EditorSetup.RegisterDataProvider();
        EditorSetup.RegisterCommands();
        EditorSetup.RegisterMetrics();

        EngineCommandHandler.CommandQueues = editorQueues;
    }


    public void Update(float delta)
    {
        if (!Enabled) return;
        _debugTools.Update(delta);
    }

    public void RenderMetricsUi()
    {
        if (!Enabled) return;
        _debugTools.Render();
    }

    public void RefreshMetrics(bool force = false)
    {
        if (!Enabled) return;
        if (force)
        {
            MetricsApi.RefreshFrameMetrics();
            MetricsApi.RefreshAssetMetrics();
            MetricsApi.RefreshGfxResourceMetrics();

            MetricsApi.RefreshSceneMetrics();
            MetricsApi.RefreshMemoryMetrics();
            return;
        }

        if (_ticker++ >= 4)
        {
            MetricsApi.RefreshFrameMetrics();
            _ticker = 0;
        }

        switch (_mediumTicker)
        {
            case 5: MetricsApi.RefreshSceneMetrics(); break;
            case 10: MetricsApi.RefreshGfxResourceMetrics(); break;
            case 15: MetricsApi.RefreshAssetMetrics(); break;
        }

        if (_mediumTicker++ >= 15) _mediumTicker = 0;

        if (_slowTicker++ >= 30)
        {
            _slowTicker = 0;
            MetricsApi.RefreshMemoryMetrics();
        }
    }

    public void DrainLogs()
    {
        if (_drainGfxLogs && GfxLog.Count > 0)
        {
            var logs = GfxLog.DrainLogs();
            foreach (ref readonly var log in logs)
                _debugTools.AddLog(_logParser.Format(in log));
        }

        if (!_drainGfxLogs && Logger.Count > 0)
        {
            var logs = Logger.DrainLogs();
            foreach (ref readonly var log in logs)
                _debugTools.AddLog(_logParser.Format(in log));
        }

        _drainGfxLogs = !_drainGfxLogs;
    }

    public void Dispose()
    {
        Enabled = false;
        _debugTools.Dispose();
    }

    private static class EditorSetup
    {
        public static EditorPortal DebugTools = null!;

        public static void ProcessStringLog(StringLogEvent log) => DebugTools.AddLog(_logParser.Format(log));

        public static void AttachEditor(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
        {
            MetricRouter.Attach(world, assetSystem, frameInfo);
            EngineDataProvider.Attach(world, assetSystem);
        }

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

        public static unsafe void RegisterDataProvider()
        {
            EditorApi.FetchAssetStoreData = EngineDataProvider.GetAssetStoreData;
            EditorApi.FetchAssetObjectFiles = EngineDataProvider.GetAssetObjectFiles;
            EditorApi.FetchEntityView = EngineDataProvider.GetEntityView;

            EditorApi.UpdateEntityData = new ApiDataRequest<EntityDataPayload>(
                &EngineDataProvider.FillEntityData, &EngineDataProvider.WriteToEntity);
            EditorApi.UpdateCameraData = new ApiDataRequest<CameraEditorPayload>(
                &EngineDataProvider.FillCameraData, &EngineDataProvider.WriteCameraData);
            EditorApi.UpdateWorldParams = new ApiDataRequest<WorldParamState>(
                &EngineDataProvider.FillWorldParams, &EngineDataProvider.WriteWorldParams);
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