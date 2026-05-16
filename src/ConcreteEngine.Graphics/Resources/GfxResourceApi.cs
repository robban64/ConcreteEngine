using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

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
    public NativeHandle GetNativeHandle<TId, TMeta>(TId id) where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
    {
        var handle = _storeHub.GetStore<TId, TMeta>().GetHandle(id);
        return _backendHub.GetStore(handle.Kind).GetNativeHandle(handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GfxRef GetTextureGfxRef(TextureId id) 
    {
        var handle = _storeHub.TextureStore.GetHandle(id);
        return new GfxRef(id, handle.Gen, handle.Kind);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeHandle GetTextureGpuHandle(GfxRef refHandle)
    {
        var handle = _storeHub.TextureStore.GetHandleRaw(refHandle.ResourceId);
        if(!refHandle.ValidateHandle(handle)) Throwers.InvalidHandle(refHandle);
        return _backendHub.TextureStore.GetNativeHandle(handle);
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