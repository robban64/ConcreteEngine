#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendOps
{
    ResourceKind Kind { get; }
    void Delete(in GfxHandle handle);
}

internal sealed class BackendOps<TId, THandle, TMeta, TDef> : IBackendOps
    where TId : unmanaged, IResourceId
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    where TMeta : unmanaged, IResourceMeta
    where TDef : unmanaged, IResourceRefToken<TId, THandle, TMeta>
{
    private readonly BackendStoreFacade<THandle> _facade;
    public ResourceKind Kind => TDef.Kind;

    public BackendOps(BackendStoreHub storeHub) => _facade = storeHub.Get<TId, THandle, TMeta, TDef>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle Get(in GfxHandle handle) => _facade.Get(in handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetRef(GfxRefToken<TId> refToken) => _facade.GetRef(refToken);


    public GfxRefToken<TId> AddExisting(uint rawHandle, in TMeta meta, out TMeta outMeta)
    {
        var th = TDef.MakeHandle(rawHandle);
        var gfx = _facade.Add(th);
        outMeta = meta;
        return new GfxRefToken<TId>(gfx);
    }

    public GfxRefToken<TId> Add(THandle handle)
    {
        var gfx = _facade.Add(handle);
        return new GfxRefToken<TId>(gfx);
    }

    public void Delete(in GfxHandle handle) => _facade.Remove(in handle);
}