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

internal delegate void BackendDelete<THandle>(in GfxHandle handle, bool replace)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal delegate GfxHandle BackendReplace<in THandle>(in GfxHandle gfxHandle, THandle handle)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal interface IResourceBackendDispatcher;

internal sealed class ResourceBackendDispatcher<THandle> : IResourceBackendDispatcher
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public BackendCreate<THandle> OnCreated { get; }
    public BackendDelete<THandle> OnDelete { get; }
    public BackendReplace<THandle> OnReplace { get; }

    public ResourceBackendDispatcher(BackendCreate<THandle> onCreated, BackendDelete<THandle> onDelete,
        BackendReplace<THandle> onReplace)
    {
        OnCreated = onCreated;
        OnDelete = onDelete;
        OnReplace = onReplace;
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

    public void OnDelete<THandle>(in GfxHandle gfxHandle, bool replace)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.TryGetValue(gfxHandle.Kind, out var dispatcher) &&
            dispatcher is ResourceBackendDispatcher<THandle> backendDispatcher)
        {
            backendDispatcher.OnDelete(gfxHandle, replace);
            return;
        }

        throw new ArgumentException($"Unknown resource kind: {gfxHandle.Kind} ({gfxHandle.ToString()})");
    }

    public GfxHandle OnReplace<THandle>(in GfxHandle gfxHandle, THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.TryGetValue(gfxHandle.Kind, out var dispatcher) &&
            dispatcher is ResourceBackendDispatcher<THandle> backendDispatcher)
        {
            return backendDispatcher.OnReplace(in gfxHandle, handle);
        }

        throw new ArgumentException($"Unknown resource kind: {gfxHandle.Kind} ({gfxHandle.ToString()})");
    }
}

internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly ResourceStoreHub _resourceStores;
    private readonly BackendStoreHub _backendHub;

    private readonly GfxResourceDisposer _disposer;

    private readonly BackendDriverDispatcher _backendDispatcher;

    private readonly Dictionary<ResourceKind, IResourceBackendDispatcher> _dispatchers = new(8);

    internal BackendStoreHub BackendStoreHub => _backendHub;
    
    internal BackendDriverDispatcher GetDispatcher() =>  _backendDispatcher;

    public GfxResourceManager()
    {
        _resourceStores = new ResourceStoreHub();
        _backendHub = new BackendStoreHub();

        RegisterGlDispatcher();
        _backendDispatcher = new BackendDriverDispatcher(_dispatchers);
    }

    internal void AttachStore(IGraphicsDriver driver)
    {
        _backendHub.AttachToDriver(driver);
    }

    internal void AttachDispatchers(IGraphicsDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver, nameof(driver));
        ArgumentNullException.ThrowIfNull(_backendDispatcher, nameof(_backendDispatcher));
        ArgumentOutOfRangeException.ThrowIfEqual(_dispatchers.Count, 0);

        driver.AttachDispatcher(_backendDispatcher);
    }

    private void RegisterGlDispatcher()
    {
        /*
        RegisterDispatcher<GlTextureHandle>(ResourceKind.Texture, OnCreate, OnDelete<GlTextureHandle>);
        RegisterDispatcher<GlShaderHandle>(ResourceKind.Shader, OnCreate, OnDelete<GlShaderHandle>);
        RegisterDispatcher<GlMeshHandle>(ResourceKind.Mesh, OnCreate, OnDelete<GlMeshHandle>);
        RegisterDispatcher<GlVboHandle>(ResourceKind.VertexBuffer, OnCreate, OnDelete<GlVboHandle>);
        RegisterDispatcher<GlIboHandle>(ResourceKind.IndexBuffer, OnCreate, OnDelete<GlIboHandle>);
        RegisterDispatcher<GlFboHandle>(ResourceKind.FrameBuffer, OnCreate, OnDelete<GlFboHandle>);
        RegisterDispatcher<GlRboHandle>(ResourceKind.RenderBuffer, OnCreate, OnDelete<GlRboHandle>);
        RegisterDispatcher<GlUboHandle>(ResourceKind.UniformBuffer, OnCreate, _backendHub.Remove);
*/
        RegisterDispatcher<GlTextureHandle>(ResourceKind.Texture);
        RegisterDispatcher<GlShaderHandle>(ResourceKind.Shader);
        RegisterDispatcher<GlMeshHandle>(ResourceKind.Mesh);
        RegisterDispatcher<GlVboHandle>(ResourceKind.VertexBuffer);
        RegisterDispatcher<GlIboHandle>(ResourceKind.IndexBuffer);
        RegisterDispatcher<GlFboHandle>(ResourceKind.FrameBuffer);
        RegisterDispatcher<GlRboHandle>(ResourceKind.RenderBuffer);
        RegisterDispatcher<GlUboHandle>(ResourceKind.UniformBuffer);
    }
    


    private GfxHandle OnCreate<THandle>(ResourceKind kind, THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        return _backendHub.Add(kind, handle);
    }
    
    private void OnDelete<THandle>(in GfxHandle handle, bool replace)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        _backendHub.Remove(handle, replace);
    }
    
    private GfxHandle OnReplace<THandle>(in GfxHandle gfxHandle, THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        return _backendHub.Replace(gfxHandle, handle);
    }

    private void RegisterDispatcher<THandle>(ResourceKind kind)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.ContainsKey(kind))
            throw new ArgumentException($"Dispatcher '{kind}' is already registered.", nameof(kind));

        var value = new ResourceBackendDispatcher<THandle>(_backendHub.Add, _backendHub.Remove, _backendHub.Replace);
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