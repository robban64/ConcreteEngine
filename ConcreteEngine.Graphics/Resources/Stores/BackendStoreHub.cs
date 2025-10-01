namespace ConcreteEngine.Graphics.Resources;

internal sealed class ResourceBackendDispatcher
{
    public required BackendDelete OnDelete { get; init; }
}

internal sealed class BackendStoreHub
{
    private readonly Dictionary<ResourceKind, IBackendStoreFacade> _stores = new(8);
    private readonly BackendOpsHub _backendOps;

    internal BackendOpsHub BackendOps => _backendOps;

    public BackendStoreHub()
    {
        RegisterBackendStores();
        _backendOps = new BackendOpsHub(this);
    }


    public void Register<TId, THandle, TMeta, TDef>(BackendResourceStore<THandle> store)
        where TId : unmanaged, IResourceId
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
        where TMeta : unmanaged, IResourceMeta
        where TDef : unmanaged, IResourceRefToken<TId, THandle, TMeta>
    {
        if (!_stores.TryAdd(TDef.Kind, new BackendStoreFacade<THandle>(store)))
            throw new InvalidOperationException("Duplicate backend store.");
    }

    internal IBackendStoreFacade Get(ResourceKind kind)
    {
        if (!_stores.TryGetValue(kind, out var store))
            throw new InvalidOperationException("Missing backend store.");

        return store;
    }

    public BackendStoreFacade<THandle> Get<TId, THandle, TMeta, TDef>()
        where TId : unmanaged, IResourceId
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
        where TMeta : unmanaged, IResourceMeta
        where TDef : unmanaged, IResourceRefToken<TId, THandle, TMeta>
    {
        if (!_stores.TryGetValue(TDef.Kind, out var store))
            throw new InvalidOperationException("Missing backend store.");

        if (store is not BackendStoreFacade<THandle> facade)
        {
            throw new InvalidOperationException(
                $"Backend store is of wrong type. Excepting {typeof(BackendStoreFacade<THandle>).Name}");
        }

        return facade;
    }

    private void RegisterBackendStores()
    {
        Register<TextureId, GlTextureHandle, TextureMeta, TextureDef>(
            new(ResourceKind.Texture));

        Register<ShaderId, GlShaderHandle, ShaderMeta, ShaderDef>(
            new(ResourceKind.Shader));

        Register<MeshId, GlMeshHandle, MeshMeta, MeshDef>(
            new(ResourceKind.Mesh));

        Register<VertexBufferId, GlVboHandle, VertexBufferMeta, VertexBufferDef>(
            new(ResourceKind.VertexBuffer));

        Register<IndexBufferId, GlIboHandle, IndexBufferMeta, IndexBufferDef>(
            new(ResourceKind.IndexBuffer));

        Register<FrameBufferId, GlFboHandle, FrameBufferMeta, FrameBufferDef>(
            new(ResourceKind.FrameBuffer));

        Register<RenderBufferId, GlRboHandle, RenderBufferMeta, RenderBufferDef>(
            new(ResourceKind.RenderBuffer));

        Register<UniformBufferId, GlUboHandle, UniformBufferMeta, UniformBufferDef>(
            new(ResourceKind.UniformBuffer));
    }
}