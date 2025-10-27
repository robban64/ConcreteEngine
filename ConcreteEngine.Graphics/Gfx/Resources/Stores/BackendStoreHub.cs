#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal sealed class ResourceBackendDispatcher
{
    public required BackendDeleteDel OnDelete { get; init; }
}

internal sealed class BackendStoreHub
{
    private readonly Dictionary<ResourceKind, IBackendResourceStore> _stores = new(8);

    internal BackendStoreBundle StoreBundle { get; }

    public BackendStoreHub()
    {
        RegisterBackendStores();
        StoreBundle = new BackendStoreBundle(this);
    }


    public void Register<TId, THandle>(BackendResourceStore<TId, THandle> store)
        where TId : unmanaged, IResourceId where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (!_stores.TryAdd(store.Kind, store))
            throw new InvalidOperationException("Duplicate backend store.");
    }

    internal IBackendResourceStore GetStore(ResourceKind kind)
    {
        if (!_stores.TryGetValue(kind, out var store))
            throw new InvalidOperationException("Missing backend store.");

        return store;
    }

    public BackendResourceStore<TId, THandle> GetStore<TId, THandle>()
        where TId : unmanaged, IResourceId where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (!_stores.TryGetValue(TId.Kind, out var s) || s is not BackendResourceStore<TId, THandle> store)
            throw new InvalidOperationException("Missing backend store.");

        return store;
    }

    private void RegisterBackendStores()
    {
        Register(new BackendResourceStore<TextureId, GlTextureHandle>(ResourceKind.Texture));
        Register(new BackendResourceStore<ShaderId, GlShaderHandle>(ResourceKind.Shader));
        Register(new BackendResourceStore<MeshId, GlMeshHandle>(ResourceKind.Mesh));
        Register(new BackendResourceStore<VertexBufferId, GlVboHandle>(ResourceKind.VertexBuffer));
        Register(new BackendResourceStore<IndexBufferId, GlIboHandle>(ResourceKind.IndexBuffer));
        Register(new BackendResourceStore<FrameBufferId, GlFboHandle>(ResourceKind.FrameBuffer));
        Register(new BackendResourceStore<RenderBufferId, GlRboHandle>(ResourceKind.RenderBuffer));
        Register(new BackendResourceStore<UniformBufferId, GlUboHandle>(ResourceKind.UniformBuffer));
    }
}