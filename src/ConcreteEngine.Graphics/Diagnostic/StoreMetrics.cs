using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utility;
using static ConcreteEngine.Graphics.Diagnostic.MetaMetricController;

namespace ConcreteEngine.Graphics.Diagnostic;

internal interface IStoreMetrics
{
    GraphicsKind Kind { get; }
    string Name { get; }
    string ShortName { get; }

    void GetResult(out GfxStoreMeta data);
}

internal sealed class StoreMetrics<TMeta>(
    GraphicsKind kind,
    GfxResourceStore<TMeta> gfxStore,
    BackendResourceStore backendStore)
    : IStoreMetrics where TMeta : unmanaged, IResourceMeta

{
    public GraphicsKind Kind { get; } = kind;
    public string Name { get; } = kind.ToResourceName();
    public string ShortName { get; } = kind.ToShortText();

    private GfxStoreMeta _data;

    public void GetResult(out GfxStoreMeta data)
    {
        var gfx = gfxStore;
        var bk = backendStore;

        _data.Fk = new CollectionSample(gfx.Count, gfx.Capacity, gfx.GetAliveCount(), gfx.FreeCount);
        _data.Bk = new CollectionSample(bk.Count, bk.Capacity, bk.GetAliveCount(), bk.FreeCount);
        _data.MetaInfo = GetSpecialMetric();
        _data.Kind = Kind;
        data = _data;
    }

    private GfxMetaInfo GetSpecialMetric()
    {
        return gfxStore switch
        {
            GfxResourceStore<TextureMeta> texStore =>
                GetTextureMetric(texStore.GetMetaSpan()),
            GfxResourceStore<ShaderMeta> shaderStore =>
                GetShaderMetric(shaderStore.GetMetaSpan()),
            GfxResourceStore<MeshMeta> meshStore =>
                GetMeshMetric(meshStore.GetMetaSpan()),
            GfxResourceStore<VertexBufferMeta> vertexBufferStore =>
                GetVboMetric(vertexBufferStore.GetMetaSpan()),
            GfxResourceStore<IndexBufferMeta> indexBufferStore =>
                GetIboMetric(indexBufferStore.GetMetaSpan()),
            GfxResourceStore<UniformBufferMeta> uniformBufferStore =>
                GetUboMetric(uniformBufferStore.GetMetaSpan()),
            GfxResourceStore<FrameBufferMeta> frameBufferStore =>
                GetFboMetric(frameBufferStore.GetMetaSpan()),
            GfxResourceStore<RenderBufferMeta> renderBufferStore =>
                GetRboMetric(renderBufferStore.GetMetaSpan()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}