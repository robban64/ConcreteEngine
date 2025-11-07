#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Editor.Diagnostics;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Graphics.Diagnostic;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using static ConcreteEngine.Editor.CommandDispatcher;

#endregion

namespace ConcreteEngine.Core.Editor;

internal sealed class EngineGateway : IDisposable
{
    private static DebugToolsSystem _debugTools = null!;
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

        _debugTools = new DebugToolsSystem(gl, window, inputCtx);
        _logParser = new LogParser();
    }

    public bool HasBindings => HasBoundCommands || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;
    public bool BlockInput() => Enabled && _debugTools.BlockInput();
    public void ToggleEngineLogger(bool enabled) => Logger.Enabled = enabled;
    public void ToggleGfxLogger(bool enabled) => GfxLog.Enabled = enabled;

    public static void SetupLogger()
    {
        if (Logger.Enabled && !Logger.IsAttached) Logger.Attach(EditorSetup.ProcessStringLog);
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

    public void DrainLogs(int n = 6)
    {
        int idx = 0;
        if (_drainGfxLogs)
        {
            while (idx < n && GfxLog.LogQueue.Count > 0)
            {
                var cmd = GfxLog.LogQueue.Dequeue();
                _debugTools.DevConsole.AddLog(_logParser.Format(cmd));
                idx++;
            }
        }

        while (idx < n && Logger.LogQueue.Count > 0)
        {
            var cmd = Logger.LogQueue.Dequeue();
            _debugTools.DevConsole.AddLog(_logParser.Format(cmd));
            idx++;
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
        public static DebugToolsSystem DebugTools = null!;

        public static void ProcessStringLog(StringLogEvent log) => DebugTools.DevConsole.AddLog(_logParser.Format(log));

        public static void AttachEditor(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
        {
            MetricRouter.Attach(world, assetSystem, frameInfo);
            EngineDataProvider.Attach(world, assetSystem);
        }

        public static void RegisterCommands()
        {
            // Editor commands
            RegisterEditorCmd<EditorTransformPayload>(CoreCmdNames.EntityTransform, EditorCommandScope.Editor, 
                EngineCommandHandler.OnEntityTransformCmd);

            RegisterEditorCmd<CameraEditorPayload>(CoreCmdNames.CameraTransform, EditorCommandScope.Editor, 
                EngineCommandHandler.OnCameraDataCmd);

            RegisterEditorCmd<EditorShaderPayload>(CoreCmdNames.AssetShader, EditorCommandScope.Engine,
                EngineCommandHandler.OnAssetShaderCmd);
            
            RegisterEditorCmd<EditorShadowPayload>(CoreCmdNames.WorldShadow, EditorCommandScope.Engine, 
                EngineCommandHandler.OnWorldShadowCmd);

            // Console commands
            RegisterConsoleCmd<EditorShaderPayload>(CoreCmdNames.AssetShader, string.Empty,
                CommandParser.ParseShaderRequest);
            
            RegisterConsoleCmd<EditorShadowPayload>(CoreCmdNames.WorldShadow, string.Empty,
                CommandParser.ParseShadowRequest);

            // Misc
            RegisterNoOpConsoleCmd("inspect-structs", string.Empty,
                EngineCommandHandler.OnStructSizesCmd);
        }

        public static void RegisterDataProvider()
        {
            EditorApi.FillAssetStoreView = EngineDataProvider.PullAssetStoreData;
            EditorApi.FetchAssetObjectFiles = EngineDataProvider.PullAssetObjectFiles;
            EditorApi.FillEntityView = EngineDataProvider.PullEntityView;
            EditorApi.FetchCameraData = EngineDataProvider.PullCameraView;
        }


        public static void RegisterMetrics()
        {
            MetricsApi.PullFrameMetrics = MetricRouter.GetFrameMetrics;
            MetricsApi.PullMaterialMetrics = MetricRouter.GetMaterialMetrics;
            MetricsApi.PullSceneMetrics = MetricRouter.GetSceneMetrics;
            MetricsApi.PullMemoryMetrics = MetricRouter.GetMemoryMetrics;
            MetricsApi.FillAssetMetrics = MetricRouter.DrainAssetStoreMetrics;
            MetricsApi.FillGfxStoreMetrics = MetricRouter.DrainGfxStoreMetrics;
        }
    }


    /*
    private static ConsoleCommandReqDel CmdWrapper(ConsoleCommandReqDel del)
    {
        return (ctx, req) =>
        {
            try
            {
                del(ctx, req);
            }
            catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
            {
                ctx.AddLog(ErrorUtils.ErrorMessageFor(ex));
            }
        };
    }
*/
}