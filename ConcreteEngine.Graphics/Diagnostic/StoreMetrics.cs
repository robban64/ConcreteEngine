#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Shared.Diagnostics;

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

internal sealed class StoreMetrics<TMeta>(
    ResourceKind kind,
    IGfxMetaResourceStore<TMeta> gfxStore,
    IBackendResourceStore backendStore)
    : IStoreMetrics where TMeta : unmanaged, IResourceMeta

{
    public ResourceKind Kind { get; } = kind;
    public string Name { get; } = kind.ToResourceName();
    public string ShortName { get; } = kind.ToShortText();

    private BasicMetric<CollectionSample> _gfxStoreMetrics;
    private BasicMetric<CollectionSample> _backendStoreMetrics;

    public ref readonly BasicMetric<CollectionSample> GfxStoreMetrics => ref _gfxStoreMetrics;
    public ref readonly BasicMetric<CollectionSample> BackendStoreMetrics => ref _gfxStoreMetrics;

    public void GetResult(out GfxStoreMetricsPayload payload)
    {
        var gfx = gfxStore;
        var bk = backendStore;

        var gfxSample = new CollectionSample(gfx.Count, gfx.Capacity, gfx.GetAliveCount(), gfx.FreeCount);
        var bkSample = new CollectionSample(bk.Count, bk.Capacity, bk.GetAliveCount(), bk.FreeCount);

        _gfxStoreMetrics = new BasicMetric<CollectionSample>(in gfxSample, default);
        _backendStoreMetrics = new BasicMetric<CollectionSample>(in bkSample, default);

        var m = GetSpecialMetric();
        var specialMeta = new TargetMetric<ValueSample>
            (m.ResourceId, new ValueSample(m.Value, m.Param2), MetricHeader.FromKind((byte)m.Kind));

        payload = new GfxStoreMetricsPayload(in _gfxStoreMetrics, in _backendStoreMetrics, in specialMeta, Kind);
    }

    private GfxMetaSpecialMetric GetSpecialMetric()
    {
        return Kind switch
        {
            ResourceKind.Texture => MetaMetricController.GetTextureMetric(((IGfxMetaResourceStore<TextureMeta>)gfxStore)
                .MetaSpan),
            ResourceKind.Shader => MetaMetricController.GetShaderMetric(((IGfxMetaResourceStore<ShaderMeta>)gfxStore)
                .MetaSpan),
            ResourceKind.Mesh => MetaMetricController.GetMeshMetric(
                ((IGfxMetaResourceStore<MeshMeta>)gfxStore).MetaSpan),
            ResourceKind.VertexBuffer => MetaMetricController.GetVboMetric(
                ((IGfxMetaResourceStore<VertexBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.IndexBuffer => MetaMetricController.GetIboMetric(
                ((IGfxMetaResourceStore<IndexBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.UniformBuffer => MetaMetricController.GetUboMetric(
                ((IGfxMetaResourceStore<UniformBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.FrameBuffer => MetaMetricController.GetFboMetric(
                ((IGfxMetaResourceStore<FrameBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.RenderBuffer => MetaMetricController.GetRboMetric(
                ((IGfxMetaResourceStore<RenderBufferMeta>)gfxStore).MetaSpan),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}