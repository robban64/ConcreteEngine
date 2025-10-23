#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Renderer.State;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Core.DebugTools;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal sealed class DebugGateway
{
    private readonly DebugService _debug;
    
    public bool HasBoundCommands {get; private set;}
    public bool HasBoundMetrics {get; private set;}
    public bool Enabled { get; private set; } = true;

    private bool _tickToggle;
    private int _ticker2 = 0, _ticker4 = 0, _ticker8 = 0;
    

    
    public DebugGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        _debug = new DebugService(gl, window, inputCtx);

        GfxDebugMetrics.ToggleLog(GfxLogSource.Store, GfxLogLayer.Backend, false);
        GfxDebugMetrics.ToggleLog(GfxLogAction.EnqueueDispose, false);
    }
    
    public bool HasBindings => HasBoundCommands || HasBoundMetrics;
    public bool Active => Enabled && HasBindings;

    public bool BlockInput() => Enabled && _debug.BlockInput();

    public void AttachDebugTools(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(assetSystem, nameof(assetSystem));
        ArgumentNullException.ThrowIfNull(frameInfo, nameof(frameInfo));
        
        DebugController.Attach(world, assetSystem, frameInfo);
    }
    
    public void RegisterCommands()
    {
        if (!Enabled) return;
        if (HasBoundCommands) throw new InvalidOperationException(nameof(HasBoundCommands));
        HasBoundCommands = true;
        
        DebugRouter.RegisterCommand("inspect-structs", DebugController.OnCmdStructSizes);
        DebugRouter.RegisterCommand("reload-shader", DebugController.OnRecreateShader);
        DebugRouter.RegisterCommand("shadow-map", DebugController.OnSetShadowMapSize);
    }
    
    public void RegisterMetrics()
    {
        if (!Enabled) return;
        if (HasBoundMetrics) throw new InvalidOperationException(nameof(HasBoundMetrics));
        HasBoundMetrics = true;

        DebugRouter.PullFrameMetrics = DebugController.GetFrameMetrics;
        DebugRouter.PullMaterialMetrics = DebugController.GetMaterialMetrics;
        DebugRouter.PullSceneMetrics = DebugController.GetSceneMetrics;
        DebugRouter.PullMemoryMetrics = DebugController.GetMemoryMetrics;

        DebugRouter.FillAssetMetrics = DebugController.DrainAssetStoreMetrics;
        DebugRouter.FillGfxStoreMetrics = DebugController.DrainGfxStoreMetrics;
    }

    public void Update(float delta)
    {
        if (!Enabled) return;
        _debug.Update(delta);
    }

    public void RenderMetricsUi()
    {
        if (!Enabled) return;
        _debug.Render();
    }

    public void RefreshMetrics(bool force = false)
    {
        if (!Enabled) return;
        if (force)
        {
            _debug.RefreshFrameMetrics();
            _debug.RefreshStoreMetrics();
            _debug.RefreshSceneMetrics();
            _debug.RefreshMemoryMetrics();
            DrainGfxLogs();
            return;
        }

        _debug.RefreshFrameMetrics();

        if (_tickToggle)
        {
            _debug.RefreshStoreMetrics();
            _debug.RefreshSceneMetrics();
        }
        else
        {
            DrainGfxLogs();
        }
        _tickToggle = !_tickToggle;

        if (++_ticker8 >= 8)
        {
            _ticker8 = 0;
            _debug.RefreshMemoryMetrics();
        }
        
    }

    private void DrainGfxLogs()
    {
        while (GfxDebugMetrics.LogQueue.Count > 0)
        {
            var cmd = GfxDebugMetrics.LogQueue.Dequeue();
            _debug.DevConsole.AddLog(cmd.ToDebugString());
        }
    }
}