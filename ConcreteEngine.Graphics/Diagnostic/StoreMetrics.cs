using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Shared.Diagnostics;
using static ConcreteEngine.Graphics.Diagnostic.MetaMetricController;

namespace ConcreteEngine.Graphics.Diagnostic;

internal interface IStoreMetrics
{
    ResourceKind Kind { get; }
    string Name { get; }
    string ShortName { get; }

    void GetResult(out GfxStoreMeta data);
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
            ResourceKind.Texture => GetTextureMetric(((IGfxMetaResourceStore<TextureMeta>)gfxStore).MetaSpan),
            ResourceKind.Shader => GetShaderMetric(((IGfxMetaResourceStore<ShaderMeta>)gfxStore).MetaSpan),
            ResourceKind.Mesh => GetMeshMetric(((IGfxMetaResourceStore<MeshMeta>)gfxStore).MetaSpan),
            ResourceKind.VertexBuffer => GetVboMetric(((IGfxMetaResourceStore<VertexBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.IndexBuffer => GetIboMetric(((IGfxMetaResourceStore<IndexBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.UniformBuffer => GetUboMetric(((IGfxMetaResourceStore<UniformBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.FrameBuffer => GetFboMetric(((IGfxMetaResourceStore<FrameBufferMeta>)gfxStore).MetaSpan),
            ResourceKind.RenderBuffer => GetRboMetric(((IGfxMetaResourceStore<RenderBufferMeta>)gfxStore).MetaSpan),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}