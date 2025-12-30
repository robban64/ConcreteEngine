using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Graphics.Gfx.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using static ConcreteEngine.Graphics.Configuration.GfxLimits;

namespace ConcreteEngine.Graphics.Gfx.Resources;

internal sealed class ResourceBackendDispatcher
{
    public required BackendDeleteDel OnDelete { get; init; }
}

internal sealed class BackendStoreHub
{
    private readonly Dictionary<GraphicsKind, IBackendResourceStore> _stores = new(8);

    internal BackendStoreBundle StoreBundle { get; }

    public BackendStoreHub()
    {
        RegisterBackendStores();
        StoreBundle = new BackendStoreBundle(this);
    }

    internal IBackendResourceStore GetStore(GraphicsKind kind)
    {
        if (!_stores.TryGetValue(kind, out var store))
            throw new InvalidOperationException("Missing backend store.");

        return store;
    }

    public BackendResourceStore<TId, THandle> GetStore<TId, THandle>()
        where TId : unmanaged, IResourceId where THandle : unmanaged, IResourceHandle
    {
        if (!_stores.TryGetValue(TId.Kind, out var s) || s is not BackendResourceStore<TId, THandle> store)
            throw new InvalidOperationException("Missing backend store.");

        return store;
    }

    private void Register<TId, THandle>(BackendResourceStore<TId, THandle> store)
        where TId : unmanaged, IResourceId where THandle : unmanaged, IResourceHandle
    {
        if (!_stores.TryAdd(store.Kind, store))
            throw new InvalidOperationException("Duplicate backend store.");
    }

    private void RegisterBackendStores()
    {
        Register(new BackendResourceStore<TextureId, GlTextureHandle>(LargeCapacity));
        Register(new BackendResourceStore<ShaderId, GlShaderHandle>(MediumCapacity));
        Register(new BackendResourceStore<MeshId, GlMeshHandle>(LargeCapacity));
        Register(new BackendResourceStore<VertexBufferId, GlVboHandle>(LargeCapacity));
        Register(new BackendResourceStore<IndexBufferId, GlIboHandle>(LargeCapacity));
        Register(new BackendResourceStore<FrameBufferId, GlFboHandle>(LowCapacity));
        Register(new BackendResourceStore<RenderBufferId, GlRboHandle>(LowCapacity));
        Register(new BackendResourceStore<UniformBufferId, GlUboHandle>(LowCapacity));
    }
}