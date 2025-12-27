using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxMetrics
{
    private static readonly GraphicsHandleKind[] Kinds = Enum.GetValues<GraphicsHandleKind>();
    private static readonly IStoreMetrics[] StoreMetrics = new IStoreMetrics[StoreCount];

    internal static GpuBufferMeta BufferMeta;
    internal static RenderFrameMeta FrameMeta;

    public static int StoreCount => Kinds.Length - 1;

    public static GpuBufferMeta GetBufferMeta() => BufferMeta;
    public static RenderFrameMeta GetFrameMeta() => FrameMeta;

    public static void DrainStoreMetrics(Span<GfxStoreMeta> span)
    {
        for (var i = 0; i < StoreMetrics.Length; i++)
            StoreMetrics[i].GetResult(out span[i]);
    }

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


    internal static void BindStore<TMeta>(
        IGfxMetaResourceStore<TMeta> gfxStore,
        IBackendResourceStore backendStore)
        where TMeta : unmanaged, IResourceMeta
    {
        var kind = gfxStore.GraphicsKind;
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, 1, nameof(kind));
        StoreMetrics[(int)kind - 1] = new StoreMetrics<TMeta>(kind, gfxStore, backendStore);
    }
}