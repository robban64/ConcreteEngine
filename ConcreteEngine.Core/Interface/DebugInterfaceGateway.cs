using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Tools.DebugInterface;

namespace ConcreteEngine.Core.Interface;

internal sealed class DebugInterfaceGateway
{
    private readonly DebugInterfaceService _debug;

    public bool HasBindings = false;

    public bool Enabled { get; set; } = true;

    public DebugInterfaceGateway(GL gl, IWindow window, IInputContext inputCtx)
    {
        _debug = new DebugInterfaceService(gl, window, inputCtx);
        _debug.Registry.RegisterFromAssemblies(typeof(GameEngine).Assembly,
            "ConcreteEngine.Core.Assets.Materials",
            "ConcreteEngine.Core.Scene");
        
        //_debug.Registry.RegisterFromAssemblies(typeof(RenderEngine).Assembly);
        //_debug.Registry.RegisterFromAssemblies(typeof(GraphicsRuntime).Assembly);
    }

    public void SetupBindings(MaterialStore materialStore, World world)
    {
        if (!Enabled) return;
        if (HasBindings) throw new InvalidOperationException(nameof(HasBindings));

        StaticDebugProvider.Bind(nameof(MaterialStore.MaterialDebugInfo), materialStore);
        StaticDebugProvider.Bind(nameof(World.EntityCount), world);
        StaticDebugProvider.Bind(nameof(World.ShadowMapSize), world);
        HasBindings = true;

        SetupConsoleCommands();
    }

    public void SetupConsoleCommands()
    {
        var console = _debug.DevConsole;
        console.RegisterCommand("structSize", DebugCommandUtils.OnCmdStructSizes);
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
        var metrics = _debug.Data.FrameMetrics;
        metrics.FrameIndex = $"FrameIdx: {frameInfo.FrameIndex} ms";
        metrics.Fps = $"FPS: {Format(frameInfo.Fps)}";
        metrics.Alpha = $"Alpha: {Format(frameInfo.Alpha)} ms";
        metrics.DrawCalls = $"Draws: {gfxFrame.DrawCalls}";
        metrics.TriangleCount = $"Verts: {gfxFrame.TriangleCount}";
        
        RefreshStore(assetStore);
        UpdateGfxStoreMetric(_debug.Data.GfxStoreMetrics);

        while (GfxDebugMetrics.LogQueue.Count > 0)
        {
            var cmd = GfxDebugMetrics.LogQueue.Dequeue();
            
            _debug.DevConsole.AddLog(cmd.ToDebugString());
            if(cmd.Detailed is not null)
                _debug.DevConsole.AddLog(cmd.Detailed);

        }

        if (HasBindings) _debug.UpdateRead();
    }

    private void RefreshStore(AssetStore assetStore)
    {
        foreach (var (k, v) in assetStore.GetAssetTypeMeta())
        {
            _debug.Data.AssetMetrics[k.Name] = (v.Count.ToString(), v.FileCount.ToString());
        }
    }

    private static void UpdateGfxStoreMetric(Dictionary<string, (string, string)> metrics)
    {
        var dict = GfxDebugMetrics.GetStoreMetrics();
        foreach (var (k, v) in dict)
        {
            var gfxStr = $"{v.GfxStoreCount}({v.GfxStoreFree})";
            var bkStr = $"{v.BackendStoreCount}({v.BackendStoreFree})";
            metrics[k.ToSimpleName()] = (gfxStr, bkStr);
        }
    }
    
    private static string Format(float value) => value.ToString("0.00");

}