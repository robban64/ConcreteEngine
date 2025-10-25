namespace Core.DebugTools.Data;

public readonly record struct DebugFrameMetrics(long FrameIndex, float Fps, float Alpha, int TriangleCount, int DrawCalls);

public readonly record struct DebugMemoryMetrics(int Allocated);

public readonly record struct DebugSceneMetrics(int EntityCount, int ShadowMapSize);

public readonly record struct DebugMaterialMetrics(int Count, int Free);

public readonly record struct DebugAssetStoreMetricRecord(string Name, int Count, int Files);

public readonly record struct DebugGfxStoreMetricsRecord(
    int Count, int Alive, int Free, int Capacity,
    in DebugGfxStoreMetricsRecord.SpecialMetric Special)
{
    public readonly record struct SpecialMetric(long Value, int ResourceId, ushort Param0 = 0, byte Kind = 0);
}

public sealed class DebugStoreMetrics(string name, string shortName, byte kind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte Kind { get; } = kind;

    public DebugGfxStoreMetricsRecord GfxStoreMetrics { get; set; }
    public DebugGfxStoreMetricsRecord BackendStoreMetrics { get; set; }
}