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

public sealed class AssetStoreTextRecord(string name, string shortName)
{
    public string? Name { get; } = name;
    public string? ShortName { get; } = shortName;
    public string? Assets { get; set; }
    public string? AssetFiles { get; set; }
}

public sealed class GfxStoreTextRecord(string name, string shortName)
{
    public string? Name { get; } = name;
    public string? ShortName { get; } = shortName;
    public TextRecord GfxStore { get; } = new();
    public TextRecord BkStore { get; } = new();

    public sealed class TextRecord
    {
        public string? StoreCount { get; set; }
        public string? StoreAliveCap { get; set; }
        public string? SpecialMetric { get; set; }
    }
}