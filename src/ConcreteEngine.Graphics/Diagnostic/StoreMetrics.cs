using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
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
    IGfxMetaResourceStore<TMeta> gfxStore,
    IBackendResourceStore backendStore)
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
        return Kind switch
        {
            GraphicsKind.Texture => GetTextureMetric(((IGfxMetaResourceStore<TextureMeta>)gfxStore).MetaSpan),
            GraphicsKind.Shader => GetShaderMetric(((IGfxMetaResourceStore<ShaderMeta>)gfxStore).MetaSpan),
            GraphicsKind.Mesh => GetMeshMetric(((IGfxMetaResourceStore<MeshMeta>)gfxStore).MetaSpan),
            GraphicsKind.VertexBuffer => GetVboMetric(
                ((IGfxMetaResourceStore<VertexBufferMeta>)gfxStore).MetaSpan),
            GraphicsKind.IndexBuffer => GetIboMetric(((IGfxMetaResourceStore<IndexBufferMeta>)gfxStore).MetaSpan),
            GraphicsKind.UniformBuffer => GetUboMetric(((IGfxMetaResourceStore<UniformBufferMeta>)gfxStore)
                .MetaSpan),
            GraphicsKind.FrameBuffer => GetFboMetric(((IGfxMetaResourceStore<FrameBufferMeta>)gfxStore).MetaSpan),
            GraphicsKind.RenderBuffer => GetRboMetric(
                ((IGfxMetaResourceStore<RenderBufferMeta>)gfxStore).MetaSpan),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}