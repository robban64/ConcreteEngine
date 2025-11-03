#region

using ConcreteEngine.Common.Diagnostics;
using Core.DebugTools.Utils;

#endregion

namespace Core.DebugTools.Data;

public sealed class MetricReport
{
    public DebugFrameMetricsText FrameMetrics { get; } = new();
    public DebugSceneMetricsText SceneMetrics { get; } = new();
    public string? MaterialMetrics { get; set; }
    public string? MemoryMetrics { get; set; }
    public List<GfxStoreMetricTextRecord> GfxStoreMetrics { get; } = new(8);
    public List<AssetStoreMetricTextRecord> AssetMetrics { get; } = new(8);


    internal void UpdateFrameMetrics(in FrameMetric<RenderInfoSample> m)
    {
        var sample = m.Sample;
        FrameMetrics.FrameIndex = $"FrameId: {m.FrameId}";
        FrameMetrics.Fps = $"FPS: {Format(sample.Fps)}";
        FrameMetrics.Alpha = $"Alpha: {Format(sample.Alpha)} ms";
        FrameMetrics.DrawCalls = $"Draws: {sample.Draws}";
        FrameMetrics.TriangleCount = $"Tris: {sample.Tris}";
        FrameMetrics.Passes = $"Passes: {sample.Passes}";
    }

    internal void UpdateSceneMetrics(in PairSample m)
    {
        SceneMetrics.EntityCount = $"Entities: {m.Value}";
        SceneMetrics.ShadowMapSize = $"ShadowMapSize: {m.Param0}";
    }

    internal void UpdateMaterialMetrics(in StoreMetric<CollectionSample> m)
    {
        MaterialMetrics = $"Materials: {m.Sample.Count}({m.Sample.Reserved})";
    }

    internal void UpdateAssetMetrics(DebugAssetStoreMetrics[] result)
    {
        if (AssetMetrics.Count != result.Length)
        {
            AssetMetrics.Clear();
            foreach (var it in result)
            {
                var name = it.Name == "MaterialTemplate" ? "MatTemplate" : it.Name;
                AssetMetrics.Add(new AssetStoreMetricTextRecord(name, ""));
            }
        }

        for (int i = 0; i < result.Length; i++)
        {
            var it = result[i];
            var curr = AssetMetrics[i];
            ref readonly var metrics = ref it.Metrics;
            var sample = metrics.Sample;
            curr.Assets = sample.Count.ToString();
            curr.AssetFiles = sample.Capacity.ToString();
        }
    }

    internal void UpdateGfxStoreMetrics(DebugGfxStoreMetrics[] metrics)
    {
        if (GfxStoreMetrics.Count != metrics.Length)
        {
            GfxStoreMetrics.Clear();
            foreach (var t in metrics)
                GfxStoreMetrics.Add(new GfxStoreMetricTextRecord(t.Name, t.ShortName));
        }

        for (int i = 0; i < metrics.Length; i++)
        {
            var it = metrics[i];
            var curr = GfxStoreMetrics[i];

            ref readonly var gfx = ref it.GfxStoreMetrics;
            ref readonly var bk = ref it.BackendStoreMetrics;

            curr.GfxStore.StoreCount = $"{gfx.Sample.Count}/{gfx.Sample.Reserved}";
            curr.GfxStore.StoreAliveCap = $"{gfx.Sample.Active}/{gfx.Sample.Capacity}";
            curr.GfxStore.SpecialMetric = MetricsFormatter.FormatSpecialMetaMetric(in it.SpecialMetric);

            curr.BkStore.StoreCount = $"{bk.Sample.Count}/{bk.Sample.Reserved}";
            curr.BkStore.StoreAliveCap = $"{bk.Sample.Active}/{bk.Sample.Capacity}";
        }
    }

    internal void UpdateMemoryMetrics(PairSample m)
    {
        MemoryMetrics = $"Allocated: {FormatMb(m.Value)}";
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";
    private static string Format(float value) => value.ToString("0.00");
}