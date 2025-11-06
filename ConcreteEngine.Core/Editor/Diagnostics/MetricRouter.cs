#region

using System.Diagnostics;
using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Graphics.Diagnostic;

#endregion

namespace ConcreteEngine.Core.Editor.Diagnostics;

internal static class MetricRouter
{
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
        var sample = new RenderInfoSample(f.Fps, f.Alpha, 0, gfxInfo.DrawCalls, gfxInfo.TriangleCount);
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

    internal static void DrainGfxStoreMetrics(MetricData data)
    {
        Span<GfxStoreMetricsPayload> span = stackalloc GfxStoreMetricsPayload[GfxMetrics.StoreCount];
        GfxMetrics.DrainStoreMetrics(span);

        if (data.GfxStoreMetrics.Length != span.Length)
        {
            data.GfxStoreMetrics = new DebugGfxStoreMetrics[span.Length];
            var names = GfxMetrics.GetStoreNames();
            for (int i = 0; i < names.Length; i++)
            {
                ref readonly var n = ref names[i];
                data.GfxStoreMetrics[i] = new DebugGfxStoreMetrics(n.Item1, n.Item2, (byte)(i + 1));
            }
        }

        var result = data.GfxStoreMetrics;
        for (int i = 0; i < span.Length; i++)
        {
            var res = result[i];
            ref readonly var metrics = ref span[i];
            res.GfxStoreMetrics = metrics.Fk;
            res.BackendStoreMetrics = metrics.Bk;
            res.SpecialMetric = metrics.Special;
        }
    }
}