using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendOps
{
    ResourceKind Kind { get; }
    void Delete(in GfxHandle handle);
}

internal sealed class BackendOps<TId, THandle, TMeta, TDef> : IBackendOps
    where TId     : unmanaged, IResourceId
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    where TMeta   : unmanaged, IResourceMeta
    where TDef    : IResourceRefToken<TId, THandle, TMeta>
{
    private readonly BackendStoreFacade<THandle> _store;
    public ResourceKind Kind => TDef.Kind;

    public BackendOps(BackendStoreHub storeHub) => _store = storeHub.Get<TId, THandle, TMeta, TDef>();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle Get(in GfxHandle handle) => _store.Get(in handle);

    public ResourceRefToken<TId> AddExisting(uint rawHandle, in TMeta meta, out TMeta outMeta)
    {
        var th  = TDef.MakeHandle(rawHandle);
        var gfx = _store.Add(th);
        outMeta = meta;
        return new ResourceRefToken<TId>(gfx);
    }

    public ResourceRefToken<TId> Add(THandle handle, in TMeta meta, out TMeta outMeta)
    {
        var gfx = _store.Add(handle);
        outMeta = meta;
        return new ResourceRefToken<TId>(gfx);
    }
    
    public ResourceRefToken<TId> Add(THandle handle)
    {
        var gfx = _store.Add(handle);
        return new ResourceRefToken<TId>(gfx);
    }

    public void Delete(in GfxHandle handle) => _store.Remove(in handle);
}
