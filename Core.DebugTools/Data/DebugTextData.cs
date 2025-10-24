using Core.DebugTools.Utils;

namespace Core.DebugTools.Data;

public sealed class DebugTextData
{
    public DebugFrameMetricsText FrameMetrics { get; } = new();
    public DebugSceneMetricsText SceneMetrics { get; } = new();
    public string? MaterialMetrics { get; set; }
    public string? MemoryMetrics { get; set; }
    public List<GfxStoreTextRecord> GfxStoreMetrics { get; } = new(8);
    public List<AssetStoreMetricsTextRecord> AssetMetrics { get; } = new(8);


    internal void UpdateFrameMetrics(in DebugFrameMetrics m)
    {
        FrameMetrics.FrameIndex = $"FrameIdx: {m.FrameIndex} ms";
        FrameMetrics.Fps = $"FPS: {Format(m.Fps)}";
        FrameMetrics.Alpha = $"Alpha: {Format(m.Alpha)} ms";
        FrameMetrics.DrawCalls = $"Draws: {m.DrawCalls}";
        FrameMetrics.TriangleCount = $"Verts: {m.TriangleCount}";
    }

    internal void UpdateSceneMetrics(in DebugSceneMetrics m)
    {
        SceneMetrics.EntityCount = $"Entities: {m.EntityCount}";
        SceneMetrics.ShadowMapSize = $"ShadowMapSize: {m.ShadowMapSize}";
    }

    internal void UpdateStoreMetrics(DebugDataContainer data)
    {
        MaterialMetrics = $"Materials: {data.MaterialMetrics.Count}({data.MaterialMetrics.Free})";

        AssetMetrics.Clear();
        foreach (var it in data.AssetMetrics)
        {
            var name = it.Name == "MaterialTemplate" ? "MatTemplate" : it.Name;
            AssetMetrics.Add(new AssetStoreMetricsTextRecord(it.Name, it.Count.ToString(), it.Files.ToString()));
        }

        if (GfxStoreMetrics.Count != data.GfxStoreMetrics.Count)
        {
            GfxStoreMetrics.Clear();
            foreach (var t in data.GfxStoreMetrics)
                GfxStoreMetrics.Add(new GfxStoreTextRecord(t.Name, t.ShortName));
        }

        for (int i = 0; i < data.GfxStoreMetrics.Count; i++)
        {
            var it = data.GfxStoreMetrics[i];
            var curr = GfxStoreMetrics[i];
            
            DebugGfxStoreMetricsRecord gfx = it.GfxStoreMetrics, bk = it.BackendStoreMetrics;
            curr.GfxStore.StoreCount = $"{gfx.Count}/{gfx.Free}";
            curr.GfxStore.StoreAliveCap = $"{gfx.Alive}/{gfx.Capacity}";
            curr.GfxStore.SpecialMetric = MetricsFormatter.FormatSpecialMetaMetric(gfx.Special);

            curr.BkStore.StoreCount = $"{bk.Count}/{bk.Free}";
            curr.BkStore.StoreAliveCap = $"{bk.Alive}/{bk.Capacity}";

        }
    }

    internal void UpdateMemoryMetrics(in DebugMemoryMetrics m)
    {
        MemoryMetrics = $"Allocated: {FormatMb(m.Allocated)}";
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";
    private static string Format(float value) => value.ToString("0.00");
}