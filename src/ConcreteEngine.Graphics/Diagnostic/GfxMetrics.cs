using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxMetrics
{
    public static readonly int StoreCount = EnumCache<GraphicsKind>.Count - 1;
    private static readonly IStoreMetrics[] StoreMetrics = new IStoreMetrics[StoreCount];

    public static GpuFrameMeta FrameMeta;

    public static void DrainStoreMetrics(GfxStoreMeta[] data)
    {
        for (var i = 0; i < StoreMetrics.Length; i++)
            StoreMetrics[i].GetResult(out data[i]);
    }


    internal static void BindStore<TMeta>() where TMeta : unmanaged, IResourceMeta
    {
        var kind = TMeta.ResourceKind;
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, 1, nameof(kind));

        StoreMetrics[(int)kind - 1] =
            new StoreMetrics<TMeta>(kind, GfxRegistry.GetGfxStore<TMeta>(), GfxRegistry.GetBackendStore<TMeta>());
    }
}