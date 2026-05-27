using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

public static class GfxRegistry {
    private static readonly IGfxResourceStore[] GfxStores = new IGfxResourceStore[GfxMetrics.StoreCount];
    private static readonly BackendResourceStore<GlHandle>[] BackendStores = new BackendResourceStore<GlHandle>[GfxMetrics.StoreCount];

    public static class Store<TMeta> where TMeta : unmanaged, IResourceMeta
    {
        
    }
}

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetNativeHandle<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        var handle = _storeHub.GetStore<TMeta>().GetHandle(id);
        return _backendHub.GetStore(handle.Kind).GetNativeHandle(handle);
    }

    public TMeta GetMeta<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return _storeHub.GetStore<TMeta>().GetMeta(id);
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