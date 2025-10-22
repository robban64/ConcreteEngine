using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Renderer.State;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Tools.DebugInterface;

namespace ConcreteEngine.Core.Diagnostic;

internal sealed class DebugInterfaceGateway
{
    private readonly DebugInterfaceService _debug;

    private AssetSystem? _assetSystem;
    public bool HasBindings = false;

    public bool Enabled { get; set; } = true;

    private int _ticker = 0;

    public DebugInterfaceGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        _debug = new DebugInterfaceService(gl, window, inputCtx);
        _debug.Registry.RegisterFromAssemblies(typeof(GameEngine).Assembly,
            "ConcreteEngine.Core.Assets.Materials",
            "ConcreteEngine.Core.Scene");
        
        //_debug.Registry.RegisterFromAssemblies(typeof(RenderEngine).Assembly);
        //_debug.Registry.RegisterFromAssemblies(typeof(GraphicsRuntime).Assembly);
        
        
        GfxDebugMetrics.ToggleLog(GfxLogSource.Store, GfxLogLayer.Backend, false);
        GfxDebugMetrics.ToggleLog(GfxLogAction.EnqueueDispose, false);
    }

    public void SetupCommandCallbacks(AssetSystem assetSystem)
    {
        ArgumentNullException.ThrowIfNull(assetSystem);
        
        _assetSystem = assetSystem;
        DebugCommandController.Attach(assetSystem);
        var console = _debug.DevConsole;
        console.RegisterCommand("inspect-structs", DebugCommandController.OnCmdStructSizes);
        console.RegisterCommand("reload-shader", DebugCommandController.OnRecreateShader);
        console.RegisterCommand("shadow-map", DebugCommandController.OnSetShadowMapSize);
 
    }
    
    public void SetupBindings(MaterialStore materialStore, World world)
    {
        if (!Enabled) return;
        if (HasBindings) throw new InvalidOperationException(nameof(HasBindings));

        StaticDebugProvider.Bind(nameof(MaterialStore.MaterialDebugInfo), materialStore);
        StaticDebugProvider.Bind(nameof(World.EntityCount), world);
        StaticDebugProvider.Bind(nameof(World.ShadowMapSize), world);
        HasBindings = true;
    }



    public bool BlockInput()
    {
        return Enabled && _debug.BlockInput();
    }

    public void Update(float delta)
    {
        if (!Enabled) return;
        _debug.Update(delta);
    }

    public void Render()
    {
        if (!Enabled) return;
        _debug.Render();
    }

    public void RefreshData(AssetStore assetStore, in RenderFrameInfo frameInfo, in GfxFrameResult gfxFrame)
    {
        if (!Enabled) return;

        _ticker++;
        if (_ticker == 3) RefreshDataSlow1(assetStore);
        if (_ticker == 6)
        {
            RefreshDataSlow2();
            _ticker = 0;
        }
        
        var metrics = _debug.Data.FrameMetrics;
        metrics.FrameIndex = $"FrameIdx: {frameInfo.FrameIndex} ms";
        metrics.Fps = $"FPS: {Format(frameInfo.Fps)}";
        metrics.Alpha = $"Alpha: {Format(frameInfo.Alpha)} ms";
        metrics.DrawCalls = $"Draws: {gfxFrame.DrawCalls}";
        metrics.TriangleCount = $"Verts: {gfxFrame.TriangleCount}";
        
        UpdateGfxStoreMetric(_debug.Data.GfxStoreMetrics);

        while (GfxDebugMetrics.LogQueue.Count > 0)
        {
            var cmd = GfxDebugMetrics.LogQueue.Dequeue();
            
            _debug.DevConsole.AddLog(cmd.ToDebugString());
        }

        if (HasBindings) _debug.UpdateRead();
    }

    private void RefreshDataSlow1(AssetStore assetStore)
    {
        RefreshStore(assetStore);
        _debug.UpdateSlowRead1();
    }
    
    private void RefreshDataSlow2()
    {
        _debug.UpdateSlowRead2();

    }

    private void RefreshStore(AssetStore assetStore)
    {
        foreach (var (k, v) in assetStore.GetAssetTypeMeta())
        {
            var name = k.Name;
            if(name == nameof(MaterialTemplate)) name = "MatTemplate";
            _debug.Data.AssetMetrics[name] = (v.Count.ToString(), v.FileCount.ToString());
        }
    }

    private static void UpdateGfxStoreMetric(Dictionary<string, (string, string)> metrics)
    {
        var dict = GfxDebugMetrics.GetStoreMetrics();
        foreach (var (k, v) in dict)
        {
            var gfxStr = $"{v.GfxStoreCount}({v.GfxStoreFree})";
            var bkStr = $"{v.BackendStoreCount}({v.BackendStoreFree})";
            metrics[k.ToLogName()] = (gfxStr, bkStr);
        }
    }
    
    private static string Format(float value) => value.ToString("0.00");

}