using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxMetrics
{
    public static int StoreCount => EnumCache<GraphicsHandleKind>.Count - 1;
    private static readonly IStoreMetrics[] StoreMetrics = new IStoreMetrics[StoreCount];

    internal static GpuBufferMeta BufferMeta;
    internal static RenderFrameMeta FrameMeta;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetBufferMeta(out GpuBufferMeta result) => result = BufferMeta;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetFrameMeta(out RenderFrameMeta result) => result = FrameMeta;

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