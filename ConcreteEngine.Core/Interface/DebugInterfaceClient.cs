using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.State;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Tools.DebugInterface;

namespace ConcreteEngine.Core.Interface;

internal sealed class DebugInterfaceClient
{
    private readonly DebugInterfaceService _debug;
    
    public bool Enabled { get; set; } = true;

    public DebugInterfaceClient(GL gl, IWindow window, IInputContext inputCtx)
    {
        _debug = new DebugInterfaceService(gl, window, inputCtx);
    }


    public bool BlockInput()
    {
        return Enabled && _debug.BlockInput();
    }

    public void Update(float delta)
    {
        if(!Enabled) return;
        _debug.Update(delta);
    }

    public void Render()
    {
        if(!Enabled) return;
        _debug.Render();
    }

    public void SendFrameData(in RenderFrameInfo frameInfo, in GfxFrameResult gfxFrame)
    {
        if(!Enabled) return;
        _debug.Data.FrameMetric = new DebugFrameRenderMetric(frameInfo.FrameIndex, frameInfo.Fps, frameInfo.Alpha);
        _debug.Data.GfxFrameMetric = new DebugGfxFrameMetric(gfxFrame.TriangleCount, gfxFrame.DrawCalls);
    }

    public void SendAssetData(AssetStore assetStore, IMaterialStore materialStore)
    {
        if(!Enabled) return;

        _debug.Data.Materials = (materialStore.Count, materialStore.FreeSlots);

        UpdateAssetMetrics(assetStore, _debug.Data.AssetMetrics);
        UpdateGfxStoreMetric(_debug.Data.GfxStoreMetrics);
    }

    public void SendWorldData(IWorld? world, EngineRenderSystem renderer)
    {
        if(!Enabled) return;

        if(world is null) return;
        var sceneShadowMap = world.RenderProps.Snapshot.Shadows.ShadowMapSize;
        var shadowFboSize = renderer.RenderEngine.GetRenderFbo<ShadowPassTag>(FboVariant.Default)!.Size;
        _debug.Data.ShadowMap = (sceneShadowMap,shadowFboSize.Width);
        _debug.Data.Entities = world.EntityCount;
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

    private static void UpdateAssetMetrics(AssetStore assetStore, Dictionary<string, (int, int)> metrics)
    {
        var dict = assetStore.GetAssetTypeMeta();

        foreach (var (k, v) in dict)
        {
            metrics[k.Name] = (v.Count, v.FileCount);
        }
    }
}