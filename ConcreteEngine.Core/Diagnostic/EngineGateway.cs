#region

using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Graphics.Diagnostic;
using Core.DebugTools;
using Core.DebugTools.Data;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal sealed class EngineGateway
{
    private readonly DebugToolsSystem _diagnostics;
    private readonly LogParser _logParser;

    public bool HasBoundCommands { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; } = true;

    private int _ticker = 0, _slowTicker = 0;

    private AssetSystem _assets;

    public EngineGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        _diagnostics = new DebugToolsSystem(gl, window, inputCtx);
        _logParser = new LogParser();
    }

    public bool HasBindings => HasBoundCommands || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;

    public bool BlockInput() => Enabled && _diagnostics.BlockInput();

    public void AttachDebugTools(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(assetSystem, nameof(assetSystem));
        ArgumentNullException.ThrowIfNull(frameInfo, nameof(frameInfo));

        _assets = assetSystem;
        MetricRouter.Attach(world, assetSystem, frameInfo);
        EditorRouter.Attach(world, assetSystem);
    }

    public void AttachLogger() => Logger.Attach(ProcessStringLog);
    public void AttachGfxLogger() => GfxLog.Enabled = true;

    private void ProcessStringLog(StringLogEvent log) => _diagnostics.DevConsole.AddLog(_logParser.Format(log));

    public void RegisterCommands()
    {
        if (!Enabled) return;
        if (HasBoundCommands) throw new InvalidOperationException(nameof(HasBoundCommands));
        HasBoundCommands = true;

        RouteTable.RegisterCommand("inspect-structs", CmdWrapper(CommandRouter.OnCmdStructSizes));
        RouteTable.RegisterCommand("reload-shader", CmdWrapper(CommandRouter.OnRecreateShader));
        RouteTable.RegisterCommand("shadow-map", CmdWrapper(CommandRouter.OnSetShadowMapSize));

        EditorTable.FillAssetStoreView = EditorRouter.DrainAssetStoreData;
        EditorTable.FetchAssetObjectFiles = EditorRouter.FetchAssetObjectFiles;
    }


    public void RegisterMetrics()
    {
        if (!Enabled) return;
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));
        HasBoundMetrics = true;

        RouteTable.PullFrameMetrics = MetricRouter.GetFrameMetrics;
        RouteTable.PullMaterialMetrics = MetricRouter.GetMaterialMetrics;
        RouteTable.PullSceneMetrics = MetricRouter.GetSceneMetrics;
        RouteTable.PullMemoryMetrics = MetricRouter.GetMemoryMetrics;

        RouteTable.FillAssetMetrics = MetricRouter.DrainAssetStoreMetrics;
        RouteTable.FillGfxStoreMetrics = MetricRouter.DrainGfxStoreMetrics;
    }

    public void Update(float delta)
    {
        if (!Enabled) return;
        _diagnostics.Update(delta);
    }

    public void RenderMetricsUi()
    {
        if (!Enabled) return;
        _diagnostics.Render();
    }

    public void RefreshMetrics(bool force = false)
    {
        if (!Enabled) return;
        var metrics = _diagnostics.Metrics;
        if (force)
        {
            metrics.RefreshFrameMetrics();
            metrics.RefreshAssetMetrics();
            metrics.RefreshGfxResourceMetrics();

            metrics.RefreshSceneMetrics();
            metrics.RefreshMemoryMetrics();
            DrainEngineLogs();
            DrainGfxLogs();
            return;
        }

        if (_ticker % 2 == 0) metrics.RefreshFrameMetrics();
        switch (_ticker)
        {
            case 6: metrics.RefreshSceneMetrics(); break;
            case 8: metrics.RefreshGfxResourceMetrics(); break;
            case 10: metrics.RefreshAssetMetrics(); break;
        }

        if (_ticker++ >= 12) _ticker = 0;

        if (_slowTicker++ >= 30)
        {
            _slowTicker = 0;
            metrics.RefreshMemoryMetrics();
        }
    }


    private void DrainEngineLogs()
    {
        while (Logger.LogQueue.Count > 0)
        {
            var cmd = Logger.LogQueue.Dequeue();
            _diagnostics.DevConsole.AddLog(_logParser.Format(cmd));
        }
    }

    private void DrainGfxLogs()
    {
        while (GfxLog.LogQueue.Count > 0)
        {
            var cmd = GfxLog.LogQueue.Dequeue();
            _diagnostics.DevConsole.AddLog(_logParser.Format(cmd));
        }
    }

    private static Action<DebugConsoleCtx, string?, string?> CmdWrapper(Action<DebugConsoleCtx, string?, string?> f)
    {
        return (ctx, a1, a2) =>
        {
            try
            {
                f(ctx, a1, a2);
            }
            catch (Exception ex) when (ErrorUtils.IsUserOrDataError(ex))
            {
                ctx.AddLog(ErrorUtils.ErrorMessageFor(ex));
            }
        };
    }
}