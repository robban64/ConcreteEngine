using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxMetrics
{
    private static readonly ResourceKind[] Kinds = Enum.GetValues<ResourceKind>();
    private static readonly IStoreMetrics[] StoreMetrics = new IStoreMetrics[StoreCount];
    
    internal static GpuBufferMeta BufferMeta;
    internal static RenderFrameMeta FrameMeta;
    
    public static int StoreCount => Kinds.Length - 1;
    

    public static ReadOnlySpan<(string, string)> GetStoreNames()
    {
        var names = new (string, string)[StoreCount];
        for (var i = 0; i < StoreMetrics.Length; i++)
        {
            var it = StoreMetrics[i];
            names[i] = (it.Name, it.ShortName);
        }

        return names;
    }

    public static void DrainStoreMetrics(Span<GfxStoreMetricsPayload> span)
    {
        for (var i = 0; i < StoreMetrics.Length; i++)
            StoreMetrics[i].GetResult(out span[i]);
    }


    internal static void BindStore<TMeta>(
        IGfxMetaResourceStore<TMeta> gfxStore,
        IBackendResourceStore backendStore)
        where TMeta : unmanaged, IResourceMeta
    {
        var kind = gfxStore.ResourceKind;
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, 1, nameof(kind));
        StoreMetrics[(int)kind - 1] = new StoreMetrics<TMeta>(kind, gfxStore, backendStore);
    }
}