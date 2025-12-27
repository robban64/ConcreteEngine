using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Shared.Diagnostics;
using static ConcreteEngine.Graphics.Diagnostic.MetaMetricController;

namespace ConcreteEngine.Graphics.Diagnostic;

internal interface IStoreMetrics
{
    GraphicsHandleKind Kind { get; }
    string Name { get; }
    string ShortName { get; }

    void GetResult(out GfxStoreMeta data);
}

internal sealed class StoreMetrics<TMeta>(
    GraphicsHandleKind kind,
    IGfxMetaResourceStore<TMeta> gfxStore,
    IBackendResourceStore backendStore)
    : IStoreMetrics where TMeta : unmanaged, IResourceMeta

{
    public GraphicsHandleKind Kind { get; } = kind;
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
        _data.Kind = (byte)Kind;
        data = _data;
    }

    private GfxMetaInfo GetSpecialMetric()
    {
        return Kind switch
        {
            GraphicsHandleKind.Texture => GetTextureMetric(((IGfxMetaResourceStore<TextureMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.Shader => GetShaderMetric(((IGfxMetaResourceStore<ShaderMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.Mesh => GetMeshMetric(((IGfxMetaResourceStore<MeshMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.VertexBuffer => GetVboMetric(((IGfxMetaResourceStore<VertexBufferMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.IndexBuffer => GetIboMetric(((IGfxMetaResourceStore<IndexBufferMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.UniformBuffer => GetUboMetric(((IGfxMetaResourceStore<UniformBufferMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.FrameBuffer => GetFboMetric(((IGfxMetaResourceStore<FrameBufferMeta>)gfxStore).MetaSpan),
            GraphicsHandleKind.RenderBuffer => GetRboMetric(((IGfxMetaResourceStore<RenderBufferMeta>)gfxStore).MetaSpan),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}