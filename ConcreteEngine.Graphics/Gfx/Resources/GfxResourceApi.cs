using ConcreteEngine.Graphics.Gfx.Resources.Data;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using ConcreteEngine.Graphics.Gfx.Resources.Stores;

namespace ConcreteEngine.Graphics.Gfx.Resources;

public sealed class GfxResourceApi
{
    //private readonly record struct TypePair(Type IdType, Type MetaType);
    private static readonly HashSet<Type> Receivers = new(4);

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

    public unsafe void BindMetaChanged<TId, TMeta>(delegate*<in GfxMetaChanged<TMeta>, void> receiver)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        if (!Receivers.Add(typeof(TId))) throw new InvalidOperationException("Already registered");

        var store = _storeHub.GetStore<TId, TMeta>();
        store.BindOnChangeCallback(*&receiver);
    }
}