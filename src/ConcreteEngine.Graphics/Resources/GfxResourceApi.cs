using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Resources;


public static class GfxResourceApi
{
    private static readonly HashSet<int> Receivers = new(4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeHandle GetNativeHandle<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        var handle = GfxRegistry.GetGfxStore<TMeta>().GetHandle(id);
        return GfxRegistry.GetBackendStore<TMeta>().GetSafe(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TMeta GetMeta<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GfxRegistry.GetGfxStore<TMeta>().GetMeta(id);
    }

    public static void BindMetaChanged(GraphicsKind kind, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (!Receivers.Add((int)kind))
            throw new InvalidOperationException($"{kind} Already registered");

        var store = GfxRegistry.GetGfxStore(kind);
        store.BindOnUpdateCallback(callback);
    }
}