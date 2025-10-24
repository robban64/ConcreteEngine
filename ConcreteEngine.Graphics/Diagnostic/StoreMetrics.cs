using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Graphics.Diagnostic;

internal interface IStoreMetrics
{
    ResourceKind Kind { get; }
    string Name { get; }
    string ShortName { get; }
    ref readonly GfxStoreMetricsRecord GfxStoreMetrics { get; }
    ref readonly GfxStoreMetricsRecord BackendStoreMetrics { get; }

    void GetResult(out GfxStoreMetricsPayload payload);
}

internal sealed class StoreMetrics<TId, TMeta, THandle>(
    ResourceKind kind,
    GetGfxStoreDel<TId, TMeta> getGfxStore,
    GetBackendStoreDel<TId, THandle> getBackendStore,
    GetSpecialMetric<TMeta> getSpecialMetricDel) : IStoreMetrics
    where TId : unmanaged, IResourceId
    where TMeta : unmanaged, IResourceMeta
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>

{
    private GetGfxStoreDel<TId, TMeta> GetGfxStore { get; } = getGfxStore;
    private GetBackendStoreDel<TId, THandle> GetBackendStore { get; } = getBackendStore;
    private GetSpecialMetric<TMeta> GetSpecialMetricDel { get; } = getSpecialMetricDel;

    public ResourceKind Kind { get; } = kind;
    public string Name { get; } = kind.ToLogName();
    public string ShortName { get; } = kind.ToLogName(true);

    private GfxStoreMetricsRecord _gfxStoreMetrics;
    private GfxStoreMetricsRecord _backendStoreMetrics;

    public ref readonly GfxStoreMetricsRecord GfxStoreMetrics => ref _gfxStoreMetrics;
    public ref readonly GfxStoreMetricsRecord BackendStoreMetrics => ref _gfxStoreMetrics;

    public void GetResult(out GfxStoreMetricsPayload payload)
    {
        var gfx = GetGfxStore();
        var bk = GetBackendStore();

        _gfxStoreMetrics = new GfxStoreMetricsRecord
            (gfx.Count, gfx.GetAliveCount(), gfx.FreeCount, gfx.Capacity, GetSpecialMetricDel(gfx.MetaSpan));

        _backendStoreMetrics = new GfxStoreMetricsRecord(bk.Count, bk.GetAliveCount(), bk.FreeCount, bk.Capacity, default);

        payload = new GfxStoreMetricsPayload(in _gfxStoreMetrics, in _backendStoreMetrics, Kind);
    }
}