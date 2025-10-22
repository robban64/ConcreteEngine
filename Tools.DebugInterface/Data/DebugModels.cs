namespace Tools.DebugInterface.Data;


public sealed class DebugFrameMetrics
{
    public string? FrameIndex { get; set; }
    public string? Fps { get; set; }
    public string? Alpha { get; set; }
    public string? TriangleCount { get; set; } 
    public string? DrawCalls { get; set; }
    public string? Allocated { get; set; }
}

public sealed class DebugDataContainer
{
    public DebugFrameMetrics FrameMetrics { get; init; } = new();
    public string? EntityCount { get; set; }
    public string? ShadowMapSize { get; set; }
    public string? MaterialDebugInfo { get; set; } // (int Count, int FreeSlots)
    public Dictionary<string, (string, string)> GfxStoreMetrics { get; } = new(8);
    public Dictionary<string, (string, string)> AssetMetrics { get; } = new(8);
}