using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

public static class GfxResourceApi
{
    private static readonly HashSet<int> Receivers = new(4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GfxHandle GetNativeHandle<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GfxRegistry.GetStore<TMeta>().GetHandle(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TMeta GetMeta<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GfxRegistry.GetStore<TMeta>().GetMeta(id);
    }

    public static void BindMetaChanged(GraphicsKind kind, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (!Receivers.Add((int)kind))
            throw new InvalidOperationException($"{kind} Already registered");

        var store = GfxRegistry.GetStore(kind);
        store.BindOnUpdateCallback(callback);
    }
}