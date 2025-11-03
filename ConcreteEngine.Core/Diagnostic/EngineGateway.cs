#region

using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Diagnostic.Utils;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Graphics.Diagnostic;
using Core.DebugTools;
using Core.DebugTools.Components;
using Core.DebugTools.Data;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal sealed class EngineGateway
{
    private readonly DiagnosticsService _diagnostics;
    private readonly LogParser _logParser;

    public bool HasBoundCommands { get; private set; }
    public bool HasBoundMetrics { get; private set; }
    public bool Enabled { get; private set; } = true;

    private int _ticker = 0, _slowTicker = 0;

    private AssetSystem _assets;

    public EngineGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        _diagnostics = new DiagnosticsService(gl, window, inputCtx);
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
        /* RouteTable.RegisterCommand("fbo-meta", CmdWrapper(static (ctx, arg1, arg2) =>
         {
             var opts = new JsonSerializerOptions { IncludeFields = true };

             var meta = GfxDebugMetrics.GetStoreMeta<FrameBufferId, FrameBufferMeta>(new FrameBufferId(2));
             ctx.AddLog(JsonSerializer.Serialize(meta,opts));
         }));*/
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
        if (force)
        {
            _diagnostics.RefreshFrameMetrics();
            _diagnostics.RefreshStoreMetrics();
            _diagnostics.RefreshSceneMetrics();
            _diagnostics.RefreshMemoryMetrics();
            DrainEngineLogs();
            DrainGfxLogs();
            return;
        }

        switch (_ticker++)
        {
            case 0: _diagnostics.RefreshFrameMetrics(); break;
            case 1:
                _diagnostics.RefreshStoreMetrics();
                RefreshAssetStoreView();
                break;
            case 2: _diagnostics.RefreshSceneMetrics(); break;
            case 3: DrainEngineLogs(); break;
            case 4: DrainGfxLogs(); break;
            default: _ticker = 0; break;
        }

        if (_slowTicker++ >= 30)
        {
            _slowTicker = 0;
            _diagnostics.RefreshMemoryMetrics();
        }
    }

    private void RefreshAssetStoreView()
    {
        _assets.StoreImpl.Process<Assets.Shaders.Shader>((it) => _diagnostics.AssetStoreViewModel.AssetObjects.Add(
            new AssetObjectViewModel
            {
                AssetId = it.RawId.Value,
                Name = it.Name,
                Generation = 0,
                IsCoreAsset = it.IsCoreAsset,
                Kind = it.Kind.ToLogTopic().ToLogText()
            }));
        
        _diagnostics.RefreshAssetStoreDetailed();
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