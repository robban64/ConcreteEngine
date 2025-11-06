#region

using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic.Routers;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Graphics.Diagnostic;
using Core.DebugTools;
using Core.DebugTools.Data;
using Core.DebugTools.Editor;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal sealed class EngineGateway : IDisposable
{
    private readonly DebugToolsSystem _debugTools;
    private readonly LogParser _logParser;

    public bool HasBoundCommands { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; } = true;

    private int _ticker = 0, _mediumTicker = 0, _slowTicker = 0;

    private bool _drainGfxLogs;

    private AssetSystem _assets;

    public EngineGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        _debugTools = new DebugToolsSystem(gl, window, inputCtx);
        _logParser = new LogParser();
    }

    public bool HasBindings => HasBoundCommands || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;

    public bool BlockInput() => Enabled && _debugTools.BlockInput();

    public void AttachLogger() => Logger.Attach(ProcessStringLog);
    public void AttachGfxLogger() => GfxLog.Enabled = true;

    public void AttachDebugTools(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(assetSystem, nameof(assetSystem));
        ArgumentNullException.ThrowIfNull(frameInfo, nameof(frameInfo));

        _assets = assetSystem;


        MetricRouter.Attach(world, assetSystem, frameInfo);
        EditorRouter.Attach(world, assetSystem);
        CommandRouter.world = world;
    }

    private void ProcessStringLog(StringLogEvent log) => _debugTools.DevConsole.AddLog(_logParser.Format(log));

    public void RegisterCommands()
    {
        if (!Enabled) return;
        if (HasBoundCommands) throw new InvalidOperationException(nameof(HasBoundCommands));
        HasBoundCommands = true;

        RouteTable.RegisterEditorCmd<EditorTransformPayload>(CoreCmdNames.EntityTransform, ConsoleCommandScope.Editor, CommandRouter.OnEntityTransformCmd);
        RouteTable.RegisterEditorCmd<EditorShaderPayload>(CoreCmdNames.AssetShader, ConsoleCommandScope.Engine, CommandRouter.OnAssetShaderCmd);
        RouteTable.RegisterEditorCmd<EditorShadowPayload>(CoreCmdNames.WorldShadow, ConsoleCommandScope.Engine, CommandRouter.OnWorldShadowCmd);

        
        RouteTable.RegisterConsoleCmd<EditorShaderPayload>(CoreCmdNames.AssetShader, string.Empty, CommandParser.ParseShaderRequest);
        RouteTable.RegisterConsoleCmd<EditorShadowPayload>(CoreCmdNames.WorldShadow, string.Empty, CommandParser.ParseShadowRequest);

        // Misc
        RouteTable.RegisterNoOpConsoleCmd("inspect-structs", string.Empty, CommandRouter.OnStructSizesCmd);

        //RouteTable.RegisterConsoleCmd(CoreCmdNames.AssetShader, CmdWrapper(CommandRouter.OnAssetShaderCmd));
        //RouteTable.RegisterConsoleCmd(CoreCmdNames.WorldShadow, CmdWrapper(CommandRouter.OnWorldShadowCmd));
        //RouteTable.RegisterConsoleCmd(CoreCmdNames.EntityTransform, CmdWrapper(CommandRouter.OnEntityTransformCmd));

        EditorTable.FillAssetStoreView = EditorRouter.PullAssetStoreData;
        EditorTable.FetchAssetObjectFiles = EditorRouter.PullAssetObjectFiles;
        EditorTable.FillEntityView = EditorRouter.PullEntityView;
    }


    public void RegisterMetrics()
    {
        if (!Enabled) return;
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));
        HasBoundMetrics = true;

        MetricsTable.PullFrameMetrics = MetricRouter.GetFrameMetrics;
        MetricsTable.PullMaterialMetrics = MetricRouter.GetMaterialMetrics;
        MetricsTable.PullSceneMetrics = MetricRouter.GetSceneMetrics;
        MetricsTable.PullMemoryMetrics = MetricRouter.GetMemoryMetrics;

        MetricsTable.FillAssetMetrics = MetricRouter.DrainAssetStoreMetrics;
        MetricsTable.FillGfxStoreMetrics = MetricRouter.DrainGfxStoreMetrics;
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
            MetricsTable.RefreshFrameMetrics();
            MetricsTable.RefreshAssetMetrics();
            MetricsTable.RefreshGfxResourceMetrics();

            MetricsTable.RefreshSceneMetrics();
            MetricsTable.RefreshMemoryMetrics();
            return;
        }

        if (_ticker++ >= 4)
        {
            MetricsTable.RefreshFrameMetrics();
            _ticker = 0;
        }

        switch (_mediumTicker)
        {
            case 5: MetricsTable.RefreshSceneMetrics(); break;
            case 10: MetricsTable.RefreshGfxResourceMetrics(); break;
            case 15: MetricsTable.RefreshAssetMetrics(); break;
        }

        if (_mediumTicker++ >= 15) _mediumTicker = 0;

        if (_slowTicker++ >= 30)
        {
            _slowTicker = 0;
            MetricsTable.RefreshMemoryMetrics();
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
    public void Dispose()
    {
        Enabled = false;
        _debugTools.Dispose();
    }
}