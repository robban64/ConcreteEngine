using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Shared.Diagnostics;
using static ConcreteEngine.Graphics.Gfx.Definitions.ResourceKind;

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
        GfxMetrics.BindStore(gfx.GetMetaStore<TextureMeta>(Texture), bk.GetStore(Texture));
        GfxMetrics.BindStore(gfx.GetMetaStore<ShaderMeta>(Shader), bk.GetStore(Shader));
        GfxMetrics.BindStore(gfx.GetMetaStore<MeshMeta>(Mesh), bk.GetStore(Mesh));
        GfxMetrics.BindStore(gfx.GetMetaStore<VertexBufferMeta>(VertexBuffer), bk.GetStore(VertexBuffer));
        GfxMetrics.BindStore(gfx.GetMetaStore<IndexBufferMeta>(IndexBuffer), bk.GetStore(IndexBuffer));
        GfxMetrics.BindStore(gfx.GetMetaStore<FrameBufferMeta>(FrameBuffer), bk.GetStore(FrameBuffer));
        GfxMetrics.BindStore(gfx.GetMetaStore<RenderBufferMeta>(RenderBuffer), bk.GetStore(RenderBuffer));
        GfxMetrics.BindStore(gfx.GetMetaStore<UniformBufferMeta>(UniformBuffer), bk.GetStore(UniformBuffer));
    }


    private static void OnDeleted(in DeleteResourceCommand cmd)
    {
        GfxLog.LogBackend(cmd.BackendHandle.Value, cmd.Handle, cmd.Handle.Kind.ToLogTopic(), LogAction.Destroy);
    }


    public GfxResourceApi GetGfxApi() => _resourceApi;
}