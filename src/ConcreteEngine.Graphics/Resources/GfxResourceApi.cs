using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Graphics.Resources;

public static class GfxResourceApi
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeHandle GetHandle<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GfxRegistry.GetStore<TMeta>().GetHandle(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TMeta GetMeta<TMeta>(GfxId<TMeta> id) where TMeta : unmanaged, IResourceMeta
    {
        return GfxRegistry.GetStore<TMeta>().GetMeta(id);
    }

    public static void BindMetaChanged<TMeta>(Action<int> callback) where TMeta : unmanaged, IResourceMeta
    {
        GfxRegistry.GetStore<TMeta>().BindOnUpdateCallback(callback);
    }
}