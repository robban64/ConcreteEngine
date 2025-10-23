namespace ConcreteEngine.Graphics.Gfx.Resources;

public sealed class GfxResourceApi
{
    private readonly GfxStoreHub _storeHub;

    private readonly Dictionary<TypePair, Delegate> _receivers = new();

    internal GfxResourceApi(GfxStoreHub store)
    {
        _storeHub = store;
    }

    public ref readonly TMeta GetMeta<TId, TMeta>(TId id)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        return ref _storeHub.GetStore<TId, TMeta>().GetMeta(id);
    }

    public void BindMetaChanged<TId, TMeta>(GfxMetaChangedDel<TId, TMeta> receiver)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        RegisterCallback(receiver);
        var store = _storeHub.GetStore<TId, TMeta>();
        store.BindOnChangeCallback(OnStoreChanged);
    }

    private void OnStoreChanged<TId, TMeta>(TId id, in GfxMetaChanged<TMeta> change)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        if (TryGetCallback<TId, TMeta>(out var callback))
            ((GfxMetaChangedDel<TId, TMeta>)callback).Invoke(id, in change);
    }


    private void RegisterCallback<TId, TMeta>(GfxMetaChangedDel<TId, TMeta> callback)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        var key = TypePair.Of<TId, TMeta>();
        if (!_receivers.TryAdd(key, callback)) throw new InvalidOperationException("Already registered");
    }

    private bool TryGetCallback<TId, TMeta>(out Delegate callback)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta =>
        _receivers.TryGetValue(TypePair.Of<TId, TMeta>(), out callback!);


    private readonly record struct TypePair(Type IdType, Type MetaType)
    {
        public static TypePair Of<TId, TMeta>() => new(typeof(TId), typeof(TMeta));
    }
}