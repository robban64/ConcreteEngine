using ConcreteEngine.Graphics.Diagnostic;

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
    }

    internal void OnDeleted(in DeleteResourceCommand cmd)
    {
        GfxDebugMetrics.Log(DebugLog.MakeResourceDispose(in cmd));
        Console.WriteLine($"Deleted {cmd.Handle.Kind} - Handle: {cmd.BackendHandle.Value} - Gen: {cmd.Handle.Gen}");
    }


    public GfxResourceApi GetGfxApi() => _resourceApi;
}