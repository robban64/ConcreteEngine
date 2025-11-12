namespace ConcreteEngine.Graphics.Gfx.Resources;

public sealed class GfxResourceApi
{
    private readonly record struct TypePair(Type IdType, Type MetaType);

    private readonly GfxStoreHub _storeHub;

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
        Gateway.RegisterCallback(typeof(TId), typeof(TMeta), receiver);
        var store = _storeHub.GetStore<TId, TMeta>();
        unsafe
        {
            store.BindOnChangeCallback(&Gateway.OnStoreChanged);
        }
    }

    private static class Gateway
    {
        private static readonly Dictionary<TypePair, Delegate> Receivers = new();

        public static void OnStoreChanged<TId, TMeta>(TId id, in TMeta newMeta, in TMeta oldMeta, GfxMetaChanged message)
            where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
        {
            if (TryGetCallback(typeof(TId), typeof(TMeta), out var callback))
                ((GfxMetaChangedDel<TId, TMeta>)callback).Invoke(id, in newMeta, in oldMeta, message);
        }


        public static void RegisterCallback(Type id, Type meta, Delegate callback)
        {
            var key = new TypePair(id, meta);
            if (!Receivers.TryAdd(key, callback)) throw new InvalidOperationException("Already registered");
        }

        private static bool TryGetCallback(Type id, Type meta, out Delegate callback) =>
            Receivers.TryGetValue(new TypePair(id, meta), out callback!);
    }
}