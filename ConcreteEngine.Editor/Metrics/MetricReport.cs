#region

using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Editor.Metrics;

public sealed class MetricReport
{
    public DebugFrameMetricsText FrameMetrics { get; } = new();
    public DebugSceneMetricsText SceneMetrics { get; } = new();
    public string? MaterialMetrics { get; set; }
    public string? MemoryMetrics { get; set; }
    public List<GfxStoreMetricTextRecord> GfxStoreMetrics { get; } = new(8);
    public List<AssetStoreMetricTextRecord> AssetMetrics { get; } = new(8);


    internal void UpdateFrameMetrics(in FrameMetric m, in RenderInfoSample sample)
    {
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer16);
        var strBuffer = formatter.Format(m.FrameId);

        if (!FrameMetrics.FrameIndex.AsSpan().EndsWith(strBuffer))
            FrameMetrics.FrameIndex = $"FrameId: {strBuffer.ToString()}";

        strBuffer = formatter.Format(sample.Fps);
        if (!FrameMetrics.Fps.AsSpan().EndsWith(strBuffer))
            FrameMetrics.Fps = $"FPS: {strBuffer.ToString()}";

        strBuffer = formatter.Format(sample.Alpha);
        if (!FrameMetrics.Alpha.AsSpan().EndsWith(strBuffer))
            FrameMetrics.Alpha = $"Alpha: {strBuffer.ToString()} ms";


        strBuffer = formatter.Format(sample.Draws);
        if (!FrameMetrics.DrawCalls.AsSpan().EndsWith(strBuffer))
            FrameMetrics.DrawCalls = $"Draws: {strBuffer.ToString()}";

        strBuffer = formatter.Format(sample.Tris);
        if (!FrameMetrics.TriangleCount.AsSpan().EndsWith(strBuffer))
            FrameMetrics.TriangleCount = $"Tris: {strBuffer.ToString()}";

        strBuffer = formatter.Format(sample.Passes);
        if (!FrameMetrics.Passes.AsSpan().EndsWith(strBuffer))
            FrameMetrics.Passes = $"Passes: {strBuffer.ToString()}";
    }

    internal void UpdateSceneMetrics(in PairSample m)
    {
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer16);
        var strBuffer = formatter.Format(m.Value);

        if (!SceneMetrics.EntityCount.AsSpan().EndsWith(strBuffer))
            SceneMetrics.EntityCount = $"Entities: {strBuffer.ToString()}";

        strBuffer = formatter.Format(m.Param0);
        if (!SceneMetrics.ShadowMapSize.AsSpan().EndsWith(strBuffer))
            SceneMetrics.ShadowMapSize = $"ShadowMapSize: {strBuffer.ToString()}";
    }

    internal void UpdateMaterialMetrics(in CollectionSample m)
    {
        MaterialMetrics = $"Materials: {m.Count}({m.Reserved})";
    }

    internal void UpdateAssetMetrics(DebugAssetStoreMetrics[] result)
    {
        if (AssetMetrics.Count != result.Length)
        {
            AssetMetrics.Clear();
            foreach (var it in result)
            {
                var name = it.Name.StartsWith("Mate") ? "MatTemplate" : it.Name;
                AssetMetrics.Add(new AssetStoreMetricTextRecord(name, ""));
            }
        }

        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);
        for (int i = 0; i < result.Length; i++)
        {
            var it = result[i];
            var curr = AssetMetrics[i];
            ref readonly var sample = ref it.Sample;

            var strBuff = formatter.Format(sample.Count);
            if (curr.Assets != strBuff)
                curr.Assets = strBuff.ToString();

            strBuff = formatter.Format(sample.Capacity);
            if (curr.AssetFiles != strBuff)
                curr.AssetFiles = strBuff.ToString();
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

            curr.GfxStore.StoreCount = $"{gfx.Count}/{gfx.Reserved}";
            curr.GfxStore.StoreAliveCap = $"{gfx.Active}/{gfx.Capacity}";
            curr.GfxStore.SpecialMetric =
                MetricsFormatter.FormatSpecialMetaMetric(in it.SpecialMetric, in it.SpecialSample);

            curr.BkStore.StoreCount = $"{bk.Count}/{bk.Reserved}";
            curr.BkStore.StoreAliveCap = $"{bk.Active}/{bk.Capacity}";
        }
    }

    internal void UpdateMemoryMetrics(PairSample m)
    {
        MemoryMetrics = $"Allocated: {FormatMb(m.Value)}";
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";
    private static string Format(float value) => value.ToString("0.00");
}