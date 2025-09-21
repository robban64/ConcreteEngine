namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceManager
{

}


internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly GfxStoreHub _gfxStores;
    private readonly BackendStoreHub _backendHub;

    private readonly ResourceBackendDispatcher _dispatchers;

    internal BackendStoreHub BackendStoreHub => _backendHub;
    internal GfxStoreHub GfxStoreHub => _gfxStores;
    internal ResourceBackendDispatcher BackendDispatcher => _dispatchers;

    public GfxResourceManager()
    {
        _gfxStores = new GfxStoreHub();
        _backendHub = new BackendStoreHub();

        _dispatchers = new ResourceBackendDispatcher { OnDelete = OnDeleted };
    }

    public void OnDeleted(in DeleteCmd cmd)
    {
        Console.WriteLine($"Deleted {cmd.Handle.Kind} - Id: {cmd.IdValue}");
    }
}
