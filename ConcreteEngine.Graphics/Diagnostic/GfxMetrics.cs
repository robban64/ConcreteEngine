#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public static class GfxMetrics
{
    private static readonly ResourceKind[] Kinds = Enum.GetValues<ResourceKind>();

    private static readonly IStoreMetrics[] StoreMetrics = new IStoreMetrics[StoreCount];

    //public static bool MetricsEnabled { get; private set; } = true;

    public static int StoreCount => Kinds.Length - 1;

    public static ReadOnlySpan<ResourceKind> GetResourceKinds() => Kinds;

    internal static IStoreMetrics GetStoreMetrics(ResourceKind kind) => StoreMetrics[(int)kind - 1];

    
    public static ReadOnlySpan<(string, string)> GetStoreNames()
    {
        var names = new (string, string)[StoreCount];
        for (int i = 0; i < StoreMetrics.Length; i++)
        {
            var it = StoreMetrics[i];
            names[i] = (it.Name, it.ShortName);
        }

        return names;
    }

    public static void DrainStoreMetrics(Span<GfxStoreMetricsPayload> span)
    {
        for (int i = 0; i < StoreMetrics.Length; i++)
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