using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Resources;

public sealed class GfxResourceApi
{
    private readonly GfxStoreHub _storeHub;
    internal GfxResourceApi(GfxStoreHub store)
    {
        _storeHub = store;
        ApiRegistry<TextureId, TextureMeta>.GetMeta = store.TextureStore.GetMeta;
        ApiRegistry<ShaderId, ShaderMeta>.GetMeta = store.ShaderStore.GetMeta;
        ApiRegistry<MeshId, MeshMeta>.GetMeta = store.MeshStore.GetMeta;
        ApiRegistry<VertexBufferId, VertexBufferMeta>.GetMeta = store.VboStore.GetMeta;
        ApiRegistry<IndexBufferId, IndexBufferMeta>.GetMeta = store.IboStore.GetMeta;
        ApiRegistry<FrameBufferId, FrameBufferMeta>.GetMeta = store.FboStore.GetMeta;
        ApiRegistry<RenderBufferId, RenderBufferMeta>.GetMeta = store.RboStore.GetMeta;
        ApiRegistry<UniformBufferId, UniformBufferMeta>.GetMeta = store.UboStore.GetMeta;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public  GfxResourceStore<TId, TMeta>.IdEnumerable GetEnumerator<TId, TMeta>(TId id)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        return _storeHub.GetStore<TId,TMeta>().IdEnumerator
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly TMeta GetMeta<TId, TMeta>(TId id)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        return ref ApiRegistry<TId, TMeta>.GetMeta(id);
    }

    private static class ApiRegistry<TId, TMeta>
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        public static GetMetaDel<TId, TMeta> GetMeta = null!;
    }
}