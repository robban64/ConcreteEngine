using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class GfxResourceManager : IDisposable
{
    internal ResourceBackendDispatcher BackendDispatcher { get; }

    internal GfxResourceManager()
    {
        GfxRegistry.CreateStores();

        BackendDispatcher = new ResourceBackendDispatcher { OnDelete = OnDeleted };
    }

    private static void OnDeleted(DeleteResourceCommand cmd)
    {
        GfxLog.LogBackend(cmd.Handle, cmd.GfxId, cmd.Kind.ToLogTopic(), LogAction.Destroy);
    }


    public void Dispose() => GfxRegistry.DisposeAllStores();
}