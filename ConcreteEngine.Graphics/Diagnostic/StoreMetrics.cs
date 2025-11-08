#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Shared.MetricData;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

internal interface IStoreMetrics
{
    ResourceKind Kind { get; }
    string Name { get; }
    string ShortName { get; }
    ref readonly BasicMetric<CollectionSample> GfxStoreMetrics { get; }
    ref readonly BasicMetric<CollectionSample> BackendStoreMetrics { get; }

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
    public string Name { get; } = kind.ToResourceName();
    public string ShortName { get; } = kind.ToShortText();

    private BasicMetric<CollectionSample> _gfxStoreMetrics;
    private BasicMetric<CollectionSample> _backendStoreMetrics;

    public ref readonly BasicMetric<CollectionSample> GfxStoreMetrics => ref _gfxStoreMetrics;
    public ref readonly BasicMetric<CollectionSample> BackendStoreMetrics => ref _gfxStoreMetrics;

    public void GetResult(out GfxStoreMetricsPayload payload)
    {
        var gfx = GetGfxStore();
        var bk = GetBackendStore();

        var gfxSample = new CollectionSample(gfx.Count, gfx.Capacity, gfx.GetAliveCount(), gfx.FreeCount);
        var bkSample = new CollectionSample(bk.Count, bk.Capacity, bk.GetAliveCount(), bk.FreeCount);

        _gfxStoreMetrics = new BasicMetric<CollectionSample>(in gfxSample, default);
        _backendStoreMetrics = new BasicMetric<CollectionSample>(in bkSample, default);

        var m = GetSpecialMetricDel(gfx.MetaSpan);
        var specialMeta = new TargetMetric<ValueSample>
            (m.ResourceId, new ValueSample(m.Value, m.Param2), MetricHeader.FromKind(m.Kind));

        payload = new GfxStoreMetricsPayload(in _gfxStoreMetrics, in _backendStoreMetrics, in specialMeta, Kind);
    }
}