namespace Core.DebugTools.Data;

public readonly record struct DebugFrameMetrics(
    long FrameIndex,
    float Fps,
    float Alpha,
    int TriangleCount,
    int DrawCalls);

public readonly record struct DebugSceneMetrics(int EntityCount, int ShadowMapSize);

public readonly record struct DebugMaterialMetrics(int Count, int Free);


public readonly record struct DebugGfxStoreMetricRecord(string Name, int GfxCount, int GfxFree, int BkCount, int BkFree);

public readonly record struct DebugAssetStoreMetricRecord(string Name, int Count, int Files);

public readonly record struct DebugMemoryMetrics(int Allocated);