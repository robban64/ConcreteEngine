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

        _data.Fk = new CollectionSample(gfx.Count, gfx.Length, gfx.GetAliveCount(), gfx.FreeCount);
        _data.Bk = new CollectionSample(bk.Count, bk.Length, bk.GetAliveCount(), bk.FreeCount);
        _data.MetaInfo = GetSpecialMetric();
        _data.Kind = Kind;
        data = _data;
    }

    private GfxMetaInfo GetSpecialMetric()
    {
        return Kind switch
        {
            GraphicsKind.Texture =>
                GetTextureMetric(((IGfxMetaResourceStore<TextureMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.Shader =>
                GetShaderMetric(((IGfxMetaResourceStore<ShaderMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.Mesh =>
                GetMeshMetric(((IGfxMetaResourceStore<MeshMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.VertexBuffer =>
                GetVboMetric(((IGfxMetaResourceStore<VertexBufferMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.IndexBuffer =>
                GetIboMetric(((IGfxMetaResourceStore<IndexBufferMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.UniformBuffer =>
                GetUboMetric(((IGfxMetaResourceStore<UniformBufferMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.FrameBuffer =>
                GetFboMetric(((IGfxMetaResourceStore<FrameBufferMeta>)gfxStore).GetMetaSpan()),
            GraphicsKind.RenderBuffer =>
                GetRboMetric(((IGfxMetaResourceStore<RenderBufferMeta>)gfxStore).GetMetaSpan()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}