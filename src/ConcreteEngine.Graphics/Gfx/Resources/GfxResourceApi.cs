using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Graphics.Gfx.Resources;

public sealed class GfxResourceApi
{
    private static readonly HashSet<GraphicsKind> Receivers = new(4);

    private readonly GfxStoreHub _storeHub;

    internal GfxResourceApi(GfxStoreHub store)
    {
        _storeHub = store;
    }


    public TMeta GetMeta<TId, TMeta>(TId id)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        return _storeHub.GetStore<TId, TMeta>().GetMeta(id);
    }

    public void BindMetaChanged(GraphicsKind kind, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (!Receivers.Add(kind))
            throw new InvalidOperationException($"{kind.ToString()} Already registered");

        var store = _storeHub.GetStore(kind);
        store.BindOnUpdateCallback(callback);
    }
}