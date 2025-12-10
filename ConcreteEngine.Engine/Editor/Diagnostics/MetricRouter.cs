#region

using System.Diagnostics;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Engine.Editor.Diagnostics;

internal static class MetricRouter
{
    private static World? _world;
    private static AssetSystem? _assetSystem;

    private static MaterialStore? Materials => _assetSystem?.MaterialStoreImpl;

    internal static void Attach(World world, AssetSystem assetSystem)
    {
        _world = world;
        _assetSystem = assetSystem;
    }

    internal static PairSample GetMemoryMetrics() => new((int)GC.GetAllocatedBytesForCurrentThread());

    internal static PairSample GetSceneMetrics() =>
        _world is not null ? new(_world.EntityCount, _world.ShadowMapSize) : default;

    internal static CollectionSample GetMaterialMetrics()
    {
        if (Materials is not { } m) return default;
        return new CollectionSample(m.Count, 0, 0, m.FreeSlots);
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
            res.Sample = new CollectionSample(metrics.Count, metrics.FileCount, 0);
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
            res.SpecialMetric = metrics.SpecialMetric;
            res.SpecialSample = metrics.SpecialSample;
        }
    }
}