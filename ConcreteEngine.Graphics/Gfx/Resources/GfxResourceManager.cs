#region

using ConcreteEngine.Graphics.Diagnostic;

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
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<TextureId, TextureMeta>, BackendStoreHub.GetStore<TextureId, GlTextureHandle>);
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<ShaderId, ShaderMeta>, BackendStoreHub.GetStore<ShaderId, GlShaderHandle>);
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<MeshId, MeshMeta>, BackendStoreHub.GetStore<MeshId, GlMeshHandle>);
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<VertexBufferId, VertexBufferMeta>, BackendStoreHub.GetStore<VertexBufferId, GlVboHandle>);
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<IndexBufferId, IndexBufferMeta>, BackendStoreHub.GetStore<IndexBufferId, GlIboHandle>);
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<FrameBufferId, FrameBufferMeta>, BackendStoreHub.GetStore<FrameBufferId, GlFboHandle>);
        GfxDebugMetrics.RegisterStore
            (GfxStoreHub.GetStore<RenderBufferId, RenderBufferMeta>, BackendStoreHub.GetStore<RenderBufferId, GlRboHandle>);
        GfxDebugMetrics.RegisterStore(
            GfxStoreHub.GetStore<UniformBufferId, UniformBufferMeta>,
            BackendStoreHub.GetStore<UniformBufferId, GlUboHandle>);
    }


    internal void OnDeleted(in DeleteResourceCommand cmd)
    {
        GfxDebugMetrics.Log(DebugLog.MakeResourceDispose(in cmd));
    }


    public GfxResourceApi GetGfxApi() => _resourceApi;
}