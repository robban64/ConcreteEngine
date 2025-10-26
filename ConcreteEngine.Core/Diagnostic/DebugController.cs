#region

using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.State;
using Core.DebugTools;
using Core.DebugTools.Components;
using Core.DebugTools.Data;

#endregion

namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugController
{
    //
    private static World? _world;
    private static AssetSystem? _assetSystem;
    private static RenderEngineFrameInfo? _frameInfo;

    private static MaterialStore? Materials => _assetSystem?.MaterialStoreImpl;


    internal static void Attach(World world, AssetSystem assetSystem, RenderEngineFrameInfo frameInfo)
    {
        _world = world;
        _assetSystem = assetSystem;
        _frameInfo = frameInfo;
    }

    internal static FrameMetric<RenderInfoSample> GetFrameMetrics()
    {
        if (_frameInfo is not { } f) return default;

        var gfxInfo = f.GfxResult;
        var sample = new RenderInfoSample(f.Fps, f.Alpha, 0, gfxInfo.TriangleCount, gfxInfo.DrawCalls);
        return new FrameMetric<RenderInfoSample>(f.FrameIndex, f.TimeStamp, in sample, default);
    }

    internal static PairSample GetMemoryMetrics() => new((int)GC.GetAllocatedBytesForCurrentThread());

    internal static PairSample GetSceneMetrics() =>
        _world is not null ? new(_world.EntityCount, _world.ShadowMapSize) : default;

    internal static StoreMetric<CollectionSample> GetMaterialMetrics()
    {
        if (Materials is not { } m) return default;
        var sample = new CollectionSample(m.Count, 0, 0, m.FreeSlots);
        return new StoreMetric<CollectionSample>(sample, default);
    }

    internal static void DrainAssetStoreMetrics(MetricData data)
    {
        if (_assetSystem is null) return;

        var store = _assetSystem.StoreImpl;

        if (data.AssetMetrics.Length != store.TypeCount)
        {
            data.AssetMetrics = new DebugAssetStoreMetrics[store.TypeCount];
            var names = store.GetStoreNames();
            Debug.Assert(data.AssetMetrics.Length == store.TypeCount);
            for (int i = 0; i < data.AssetMetrics.Length; i++)
                data.AssetMetrics[i] = new DebugAssetStoreMetrics(names[i], "", 1);
        }

        var result = data.AssetMetrics;
        Span<AssetTypeMetaSnapshot> span = stackalloc AssetTypeMetaSnapshot[store.TypeCount];
        store.ExtractMeta(span);
        for (int i = 0; i < span.Length; i++)
        {
            var res = result[i];
            ref readonly var metrics = ref span[i];

            var sample = new CollectionSample(metrics.Count, metrics.FileCount, 0);
            res.Metrics = new StoreMetric<CollectionSample>(sample, default);
        }
    }

    internal static void DrainGfxStoreMetrics(MetricData data) => DebugGfxController.DrainGfxStoreMetrics(data);

    public static void OnRecreateShader(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (_assetSystem is null) return;
        if (string.IsNullOrWhiteSpace(arg1) || arg1.Length < 2) return;
        _assetSystem.EnqueueRecreateShader(arg1);
        ctx.AddLog("Shader recreate enqueued");
    }

    public static void OnSetShadowMapSize(DebugConsoleCtx ctx, string? arg1, string? arg2)
    {
        if (_assetSystem is null) return;
        ArgumentNullException.ThrowIfNull(arg1, nameof(arg1));
        var size = DebugParser.IntArg(arg1);
        var shadowSize = DebugUtils.GetShadowSize(size);
        if (shadowSize <= 0)
        {
            throw new ArgumentException("Supported are 1,2,4,8 (1024, 2048, 4096, 8192)",
                nameof(arg1));
        }

        _assetSystem.EnqueueRecreateFrameBuffer(size, RecreateSpecialAction.RecreateShadowFbo);
    }


    public static void OnCmdStructSizes(DebugConsoleCtx ctx, string? _, string? __)
    {
        /*
        ctx.AddLog(StructStr<TextureSlotInfo>());
        ctx.AddLog(StructStr<Transform>());
        ctx.AddLog(StructStr<MeshComponent>());
        ctx.AddLog(StructStr<DrawCommand>());
        ctx.AddLog(StructStr<DrawCommandMeta>());
        ctx.AddLog(StructStr<MaterialUniformRecord>());
        ctx.AddLog(StructStr<DrawObjectUniform>());
        ctx.AddLog(StructStr<RecreateRequest>());
        ctx.AddLog(StructStr<GfxDebugLog>());
        */

        ctx.AddLog(StructStr<TextureMeta>());
        ctx.AddLog(StructStr<MeshMeta>());
        ctx.AddLog(StructStr<VertexBufferMeta>());
        ctx.AddLog(StructStr<IndexBufferMeta>());
        ctx.AddLog(StructStr<FrameBufferMeta>());
        ctx.AddLog(StructStr<RenderBufferMeta>());
        ctx.AddLog(StructStr<UniformBufferMeta>());

        ctx.AddLog(StructStr<StoreMetric<CollectionSample>>());
        ctx.AddLog(StructStr<GfxResourceMetric<ValueSample>>());
        
        ctx.AddLog(StructStr<GfxStoreMetricsPayload>());
        ctx.AddLog(StructStr<MaterialParams>());
        ctx.AddLog(StructStr<DrawMaterialMeta>());
        ctx.AddLog(StructStr<DrawMaterialPayload>());
        
        ctx.AddLog(StructStr<GfxPassState>());


    }

    private static string StructStr<T>() where T : unmanaged =>
        $"{typeof(T).Name,-18} - {Unsafe.SizeOf<T>().ToString()} bytes";
}