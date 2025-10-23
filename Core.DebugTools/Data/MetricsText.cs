namespace Core.DebugTools.Data;

public sealed class DebugFrameMetricsText
{
    public string? FrameIndex { get; set; }
    public string? Fps { get; set; }
    public string? Alpha { get; set; }
    public string? TriangleCount { get; set; }
    public string? DrawCalls { get; set; }
}

public sealed class DebugSceneMetricsText
{
    public string? EntityCount { get; set; }
    public string? ShadowMapSize { get; set; }
}

public sealed class DebugMaterialMetricsText
{
    public string? Count { get; set; }
    public string? Free { get; set; }
}

public sealed class DebugMemoryMetricsText
{
    public string? Allocated { get; set; } 
}

public readonly record struct AssetStoreMetricsTextRecord(string Name, string Count, string Files);

public readonly record struct GfxStoreMetricsTextRecord(string Name, string GfxStore, string BkStore);
