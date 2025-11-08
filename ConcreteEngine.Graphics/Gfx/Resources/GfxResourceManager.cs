#region

using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

public interface IGfxResourceManager
{
    GfxResourceApi GetGfxApi();
}

internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly GfxResourceApi _resourceApi;

    internal BackendStoreHub BackendStoreHub { get; }
    internal GfxStoreHub GfxStoreHub { get; }
    internal ResourceBackendDispatcher BackendDispatcher { get; }

    internal GfxResourceManager()
    {
        GfxStoreHub = new GfxStoreHub();
        BackendStoreHub = new BackendStoreHub();
        BackendDispatcher = new ResourceBackendDispatcher { OnDelete = OnDeleted };

        _resourceApi = new GfxResourceApi(GfxStoreHub);

        RegisterMetricsBindings();
    }

    private void RegisterMetricsBindings()
    {
        var (gfx, bk) = (GfxStoreHub, BackendStoreHub);
        GfxMetrics.BindStore(gfx.GetStore<TextureId, TextureMeta>, bk.GetStore<TextureId, GlTextureHandle>,
            MetaMetricController.GetTextureMetric);

        GfxMetrics.BindStore(gfx.GetStore<ShaderId, ShaderMeta>, bk.GetStore<ShaderId, GlShaderHandle>,
            MetaMetricController.GetShaderMetric);

        GfxMetrics.BindStore(gfx.GetStore<MeshId, MeshMeta>, bk.GetStore<MeshId, GlMeshHandle>,
            MetaMetricController.GetMeshMetric);

        GfxMetrics.BindStore(gfx.GetStore<VertexBufferId, VertexBufferMeta>, bk.GetStore<VertexBufferId, GlVboHandle>,
            MetaMetricController.GetVboMetric);

        GfxMetrics.BindStore(gfx.GetStore<IndexBufferId, IndexBufferMeta>, bk.GetStore<IndexBufferId, GlIboHandle>,
            MetaMetricController.GetIboMetric);

        GfxMetrics.BindStore(gfx.GetStore<FrameBufferId, FrameBufferMeta>, bk.GetStore<FrameBufferId, GlFboHandle>,
            MetaMetricController.GetFboMetric);

        GfxMetrics.BindStore(gfx.GetStore<RenderBufferId, RenderBufferMeta>, bk.GetStore<RenderBufferId, GlRboHandle>,
            MetaMetricController.GetRboMetric);

        GfxMetrics.BindStore(gfx.GetStore<UniformBufferId, UniformBufferMeta>,
            bk.GetStore<UniformBufferId, GlUboHandle>,
            MetaMetricController.GetUboMetric);
    }


    internal void OnDeleted(in DeleteResourceCommand cmd)
    {
        GfxLog.LogBackend(cmd.BackendHandle.Value, cmd.Handle, cmd.Handle.Kind.ToLogTopic(), LogAction.Destroy);
    }


    public GfxResourceApi GetGfxApi() => _resourceApi;
}