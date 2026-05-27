using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;
using static ConcreteEngine.Graphics.GraphicsKind;

namespace ConcreteEngine.Graphics.Resources;

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

        _resourceApi = new GfxResourceApi(GfxStoreHub, BackendStoreHub);

        RegisterMetricsBindings();
    }

    private void RegisterMetricsBindings()
    {
        var (gfx, bk) = (GfxStoreHub, BackendStoreHub);
        GfxMetrics.BindStore(gfx.GetStore<TextureMeta>(), bk.GetStore(Texture));
        GfxMetrics.BindStore(gfx.GetStore<ShaderMeta>(), bk.GetStore(Shader));
        GfxMetrics.BindStore(gfx.GetStore<MeshMeta>(), bk.GetStore(Mesh));
        GfxMetrics.BindStore(gfx.GetStore<VertexBufferMeta>(), bk.GetStore(VertexBuffer));
        GfxMetrics.BindStore(gfx.GetStore<IndexBufferMeta>(), bk.GetStore(IndexBuffer));
        GfxMetrics.BindStore(gfx.GetStore<FrameBufferMeta>(), bk.GetStore(FrameBuffer));
        GfxMetrics.BindStore(gfx.GetStore<RenderBufferMeta>(), bk.GetStore(RenderBuffer));
        GfxMetrics.BindStore(gfx.GetStore<UniformBufferMeta>(), bk.GetStore(UniformBuffer));
    }


    private static void OnDeleted(DeleteResourceCommand cmd)
    {
        GfxLog.LogBackend(cmd.BackendHandle, cmd.Handle, cmd.Handle.Kind.ToLogTopic(), LogAction.Destroy);
    }


    public GfxResourceApi GetGfxApi() => _resourceApi;
}