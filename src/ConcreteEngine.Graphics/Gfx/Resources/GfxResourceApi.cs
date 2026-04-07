using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources;

public sealed class GfxResourceApi
{
    private static readonly HashSet<int> Receivers = new(4);

    private readonly GfxStoreHub _storeHub;
    private readonly BackendStoreHub _backendHub;

    internal GfxResourceApi(GfxStoreHub store, BackendStoreHub backendHub)
    {
        _storeHub = store;
        _backendHub = backendHub;
    }

    public nint GetNativeHandle<TId>(TId id) where TId : unmanaged, IResourceId
    {
        var handle = _storeHub.GetHandleStore<TId>().GetHandle(id);
        return (nint)_backendHub.GetStore(handle.Kind).GetNativeHandle(handle).Value;
    }

    public TMeta GetMeta<TId, TMeta>(TId id) where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        return _storeHub.GetStore<TId, TMeta>().GetMeta(id);
    }

    public void BindMetaChanged(GraphicsKind kind, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (!Receivers.Add((int)kind))
            throw new InvalidOperationException($"{kind} Already registered");

        var store = _storeHub.GetStore(kind);
        store.BindOnUpdateCallback(callback);
    }
}