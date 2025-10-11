#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendStoreFacade
{
    ResourceKind Kind { get; }
    NativeHandle GetNative(in GfxHandle h);
}

internal sealed class BackendStoreFacade<THandle> : IBackendStoreFacade
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    private readonly BackendResourceStore<THandle> _store;

    public ResourceKind Kind => _store.Kind;

    public BackendStoreFacade(BackendResourceStore<THandle> store) => _store = store;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetNative(in GfxHandle h) => NativeHandle.From(_store.GetUntyped(in h));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetUntyped(in GfxHandle h) => _store.GetUntyped(in h);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public THandle GetHandle<TId>(GfxRefToken<TId> refToken) where TId : unmanaged, IResourceId =>
        _store.GetHandle(refToken);


    public GfxHandle Add(THandle h) => _store.Add(h);

    public void Remove(in GfxHandle h) => _store.Remove(in h);
}