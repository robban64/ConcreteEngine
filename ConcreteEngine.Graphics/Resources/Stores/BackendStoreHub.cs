namespace ConcreteEngine.Graphics.Resources;


internal interface IDriverResourceStoreCollection;

internal sealed class BackendStoreHub
{
    private Dictionary<ResourceKind, IDriverResourceStore> _stores = new(8);

    public BackendStoreHub()
    {
        
    }

    public void RemoveResource(ResourceKind kind, GfxHandle handle)
    {
        _stores[kind].Remove(handle);
    }
    
    public bool IsAlive(ResourceKind kind, GfxHandle handle)
    {
        return _stores[kind].IsAlive(handle);
    }

    public void RegisterStore<THandle>(ResourceKind key, DriverResourceStore<THandle> store) where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        _stores.Add(key, store);
    }
}