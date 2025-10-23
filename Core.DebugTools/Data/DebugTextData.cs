namespace Core.DebugTools.Data;

public sealed class DebugTextData
{
    public DebugFrameMetricsText FrameMetrics { get; } = new();
    public DebugSceneMetricsText SceneMetrics { get; } = new();
    public string? MaterialMetrics { get; set; }
    public string? MemoryMetrics { get; set; }
    public List<GfxStoreMetricsTextRecord> GfxStoreMetrics { get; } = new(8);
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

        GfxStoreMetrics.Clear();
        foreach (var it in data.GfxStoreMetrics)
        {
            var gfxStr = $"{it.GfxCount}({it.GfxFree})";
            var bkStr = $"{it.BkCount}({it.BkFree})";
            GfxStoreMetrics.Add(new GfxStoreMetricsTextRecord(it.Name, gfxStr, bkStr));
        }
    }

    internal void UpdateMemoryMetrics(in DebugMemoryMetrics m)
    {
        MemoryMetrics = $"Allocated: {FormatMb(m.Allocated)}";
    }

    private static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";
    private static string Format(float value) => value.ToString("0.00");
}