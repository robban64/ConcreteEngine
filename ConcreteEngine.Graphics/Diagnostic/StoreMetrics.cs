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

    void GetResult(out GfxStoreMetricsPayload data);
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

    private GfxStoreMetricsPayload _data;
    public ref GfxStoreMetricsPayload MetricsData => ref _data;

    public void GetResult(out GfxStoreMetricsPayload data)
    {
        var gfx = gfxStore;
        var bk = backendStore;

        _data.Fk = new CollectionSample(gfx.Count, gfx.Capacity, gfx.GetAliveCount(), gfx.FreeCount);
        _data.Bk = new CollectionSample(bk.Count, bk.Capacity, bk.GetAliveCount(), bk.FreeCount);

        var m = GetSpecialMetric();
        _data.SpecialMetric = new TargetMetric(m.ResourceId, MetricHeader.FromKind((byte)m.Kind));
        _data.SpecialSample = new ValueSample(m.Value, m.Param2);
        _data.Kind = m.Kind;
        data = _data;
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