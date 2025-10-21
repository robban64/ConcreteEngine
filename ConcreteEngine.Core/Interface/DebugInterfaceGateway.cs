using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer;
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
        _debug.Registry.RegisterFromAssemblies(typeof(GameEngine).Assembly);
        _debug.Registry.RegisterFromAssemblies(typeof(RenderEngine).Assembly);
        _debug.Registry.RegisterFromAssemblies(typeof(GraphicsRuntime).Assembly);
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
        var metrics = _debug.Data.FrameMetrics;
        metrics.FrameIndex = frameInfo.FrameIndex;
        metrics.Fps = frameInfo.Fps;
        metrics.Alpha = frameInfo.Alpha;
        metrics.DrawCalls = gfxFrame.DrawCalls;
        metrics.TriangleCount = gfxFrame.TriangleCount;

        RefreshStore(assetStore);
        UpdateGfxStoreMetric(_debug.Data.GfxStoreMetrics);

        if (HasBindings) _debug.UpdateRead();
    }

    private void RefreshStore(AssetStore assetStore)
    {
        foreach (var (k, v) in assetStore.GetAssetTypeMeta())
        {
            _debug.Data.AssetMetrics[k.Name] = (v.Count, v.FileCount);
        }
    }

    private static void UpdateGfxStoreMetric(Dictionary<string, DebugGfxStoreMetric> metrics)
    {
        var dict = GfxDebugMetrics.GetStoreMetrics();
        foreach (var (k, v) in dict)
        {
            metrics[k.ToString()] =
                new DebugGfxStoreMetric(v.GfxStoreCount, v.BackendStoreCount, v.GfxStoreFree, v.BackendStoreFree);
        }
    }
}