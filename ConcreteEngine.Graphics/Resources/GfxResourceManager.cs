namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceManager
{
    GfxResourceApi GetGfxApi();
}

internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly GfxStoreHub _gfxStores;
    private readonly BackendStoreHub _backendHub;

    private readonly ResourceBackendDispatcher _dispatchers;

    private readonly GfxResourceApi _resourceApi;

    internal BackendStoreHub BackendStoreHub => _backendHub;
    internal GfxStoreHub GfxStoreHub => _gfxStores;
    internal ResourceBackendDispatcher BackendDispatcher => _dispatchers;

    internal GfxResourceManager()
    {
        _gfxStores = new GfxStoreHub();
        _backendHub = new BackendStoreHub();
        _dispatchers = new ResourceBackendDispatcher { OnDelete = OnDeleted };

        _resourceApi = new GfxResourceApi(_gfxStores);
    }

    internal void OnDeleted(in DeleteCmd cmd)
    {
        Console.WriteLine($"Deleted {cmd.Handle.Kind} - Id: {cmd.IdValue}");
    }


    public GfxResourceApi GetGfxApi() => _resourceApi;
}