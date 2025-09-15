using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal interface IGfxResourceManager
{
    public ResourceStore<TextureId, TextureMeta> TextureStore { get; }

    public ResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }

    public ResourceStore<MeshId, MeshMeta> MeshStore { get; }

    public ResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }

    public ResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }

    public ResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }

    public ResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }

    public ResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
}

internal delegate GfxHandle BackendCreate<in THandle>(ResourceKind kind, THandle handle)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal delegate void BackendDelete<THandle>(in GfxHandle handle)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal interface IResourceBackendDispatcher;

internal sealed class ResourceBackendDispatcher<THandle> : IResourceBackendDispatcher
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public BackendCreate<THandle> OnCreated { get; set; }
    public BackendDelete<THandle> OnDelete { get; set; }

    public ResourceBackendDispatcher(BackendCreate<THandle> onCreated, BackendDelete<THandle> onDelete)
    {
        OnCreated = onCreated;
        OnDelete = onDelete;
    }
}

internal sealed class BackendDriverDispatcher
{
    private readonly Dictionary<ResourceKind, IResourceBackendDispatcher> _dispatchers;

    internal BackendDriverDispatcher(Dictionary<ResourceKind, IResourceBackendDispatcher> dispatchers)
    {
        _dispatchers = dispatchers;
    }

    public GfxHandle OnCreate<THandle>(ResourceKind kind, THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.TryGetValue(kind, out var dispatcher) &&
            dispatcher is ResourceBackendDispatcher<THandle> backendDispatcher)
        {
            return backendDispatcher.OnCreated(kind, handle);
        }
        
        throw new ArgumentException($"Unknown resource kind: {kind}");
    }
    
    public void OnDelete<THandle>(in GfxHandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.TryGetValue(handle.Kind, out var dispatcher) &&
            dispatcher is ResourceBackendDispatcher<THandle> backendDispatcher)
        {
            backendDispatcher.OnDelete(handle);
        }

        throw new ArgumentException($"Unknown resource kind: {handle.Kind} ({handle.ToString()})");
    }

}

internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly ResourceStoreHub _resourceStores;
    private readonly BackendStoreHub _backendHub;

    private readonly GfxResourceDisposer _disposer;
    
    private readonly Dictionary<ResourceKind, IResourceBackendDispatcher> _dispatchers = new(8);

    public GfxResourceManager()
    {
        _resourceStores = new ResourceStoreHub();
        _backendHub = new BackendStoreHub();

        RegisterGlDispatcher();
    }

    internal void AttachStore(IGraphicsDriver driver)
    {
        _backendHub.AttachToDriver(driver);
    }

    internal void AttachDispatchers(IGraphicsDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver, nameof(driver));
        ArgumentOutOfRangeException.ThrowIfEqual(_dispatchers.Count, 0);

        var dispatcher = new BackendDriverDispatcher(_dispatchers); 
        driver.AttachDispatcher(dispatcher);
    }

    private void RegisterGlDispatcher()
    {
        RegisterDispatcher<GlTextureHandle>(ResourceKind.Texture, OnCreate, OnDelete<GlTextureHandle>);
        RegisterDispatcher<GlShaderHandle>(ResourceKind.Shader, OnCreate, OnDelete<GlShaderHandle>);
        RegisterDispatcher<GlMeshHandle>(ResourceKind.Mesh, OnCreate, OnDelete<GlMeshHandle>);
        RegisterDispatcher<GlVboHandle>(ResourceKind.VertexBuffer, OnCreate, OnDelete<GlVboHandle>);
        RegisterDispatcher<GlIboHandle>(ResourceKind.IndexBuffer, OnCreate, OnDelete<GlIboHandle>);
        RegisterDispatcher<GlFboHandle>(ResourceKind.FrameBuffer, OnCreate, OnDelete<GlFboHandle>);
        RegisterDispatcher<GlRboHandle>(ResourceKind.RenderBuffer, OnCreate, OnDelete<GlRboHandle>);
        RegisterDispatcher<GlUboHandle>(ResourceKind.UniformBuffer, OnCreate, OnDelete<GlUboHandle>);


        return;

        void OnDelete<THandle>(in GfxHandle handle)
            where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
        {
            _backendHub.Remove(handle);
        }

        GfxHandle OnCreate<THandle>(ResourceKind kind, THandle handle)
            where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
        {
            return _backendHub.Add(kind, handle);
        }
        
        GfxHandle Replace<THandle>(GfxHandle gfxHandle, THandle handle)
            where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
        {
            return _backendHub.Replace(gfxHandle, handle);
        }
        
    }

    private void RegisterDispatcher<THandle>(ResourceKind kind, BackendCreate<THandle> onCreate,
        BackendDelete<THandle> onDelete)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.ContainsKey(kind))
            throw new ArgumentException($"Dispatcher '{kind}' is already registered.", nameof(kind));

        var value = new ResourceBackendDispatcher<THandle>(onCreate, onDelete);
        _dispatchers.Add(kind, value);
    }

    public ResourceStore<TextureId, TextureMeta> TextureStore => _resourceStores.TextureStore;
    public ResourceStore<ShaderId, ShaderMeta> ShaderStore => _resourceStores.ShaderStore;
    public ResourceStore<MeshId, MeshMeta> MeshStore => _resourceStores.MeshStore;
    public ResourceStore<VertexBufferId, VertexBufferMeta> VboStore => _resourceStores.VboStore;
    public ResourceStore<IndexBufferId, IndexBufferMeta> IboStore => _resourceStores.IboStore;
    public ResourceStore<FrameBufferId, FrameBufferMeta> FboStore => _resourceStores.FboStore;
    public ResourceStore<RenderBufferId, RenderBufferMeta> RboStore => _resourceStores.RboStore;
    public ResourceStore<UniformBufferId, UniformBufferMeta> UboStore => _resourceStores.UboStore;
}