using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;
using static ConcreteEngine.Graphics.GraphicsKind;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class GfxResourceManager : IDisposable
{
    internal ResourceBackendDispatcher BackendDispatcher { get; }

    internal GfxResourceManager()
    {
        GfxRegistry.CreateStores();

        BackendDispatcher = new ResourceBackendDispatcher { OnDelete = OnDeleted };

        RegisterMetricsBindings();
    }

    private void RegisterMetricsBindings()
    {
        GfxMetrics.BindStore<TextureMeta>();
        GfxMetrics.BindStore<ShaderMeta>();
        GfxMetrics.BindStore<MeshMeta>();
        GfxMetrics.BindStore<VertexBufferMeta>();
        GfxMetrics.BindStore<IndexBufferMeta>();
        GfxMetrics.BindStore<FrameBufferMeta>();
        GfxMetrics.BindStore<RenderBufferMeta>();
        GfxMetrics.BindStore<UniformBufferMeta>();
    }


    private static void OnDeleted(DeleteResourceCommand cmd)
    {
        GfxLog.LogBackend(cmd.BackendHandle, cmd.Handle, cmd.Handle.Kind.ToLogTopic(), LogAction.Destroy);
    }


    public void Dispose() => GfxRegistry.DisposeAllStores();
}