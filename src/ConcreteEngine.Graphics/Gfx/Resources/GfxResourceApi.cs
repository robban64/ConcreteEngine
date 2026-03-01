using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources;

public sealed class GfxResourceApi
{
    private static readonly HashSet<GraphicsKind> Receivers = new(4);

    private readonly GfxStoreHub _storeHub;
    private readonly BackendStoreHub _backendHub;

    internal GfxResourceApi(GfxStoreHub store, BackendStoreHub backendHub)
    {
        _storeHub = store;
        _backendHub = backendHub;
    }

    public nint GetNativeHandle<TId>(TId id) where TId : unmanaged, IResourceId
    {
        var handle = _storeHub.GetStore<TId>().GetHandleUntyped(id);
        return (nint)_backendHub.GetStore(TId.Kind).GetNativeHandle(handle).Value;
    }

    public TMeta GetMeta<TId, TMeta>(TId id) where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        return _storeHub.GetStore<TId, TMeta>().GetMeta(id);
    }

    public void BindMetaChanged(GraphicsKind kind, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (!Receivers.Add(kind))
            throw new InvalidOperationException($"{kind} Already registered");

        var store = _storeHub.GetStore(kind);
        store.BindOnUpdateCallback(callback);
    }
}