using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

internal interface IGfxResourceManager
{
    public FrontendResourceStore<TextureId, TextureMeta> TextureStore { get; }

    public FrontendResourceStore<ShaderId, ShaderMeta> ShaderStore { get; }

    public FrontendResourceStore<MeshId, MeshMeta> MeshStore { get; }

    public FrontendResourceStore<VertexBufferId, VertexBufferMeta> VboStore { get; }

    public FrontendResourceStore<IndexBufferId, IndexBufferMeta> IboStore { get; }

    public FrontendResourceStore<FrameBufferId, FrameBufferMeta> FboStore { get; }

    public FrontendResourceStore<RenderBufferId, RenderBufferMeta> RboStore { get; }

    public FrontendResourceStore<UniformBufferId, UniformBufferMeta> UboStore { get; }
}

internal delegate void BackendDelete<THandle>(in DeleteCmd cmd)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal interface IResourceBackendDispatcher;

internal sealed class ResourceBackendDispatcher<THandle> : IResourceBackendDispatcher
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public BackendDelete<THandle> OnDelete { get; }

    public ResourceBackendDispatcher( BackendDelete<THandle> onDelete)
    {
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

    public void OnDelete<THandle>(in DeleteCmd cmd)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var gfxHandle = cmd.Handle;
        if (_dispatchers.TryGetValue(gfxHandle.Kind, out var dispatcher) &&
            dispatcher is ResourceBackendDispatcher<THandle> backendDispatcher)
        {
            backendDispatcher.OnDelete(in cmd);
            return;
        }

        throw new ArgumentException($"Unknown resource kind: {gfxHandle.Kind} ({gfxHandle.ToString()})");
    }
}

internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly FrontendStoreHub _frontendHub;
    private readonly BackendStoreHub _backendHub;

    private readonly BackendDriverDispatcher _backendDispatcher;

    private readonly Dictionary<ResourceKind, IResourceBackendDispatcher> _dispatchers = new(8);

    internal BackendStoreHub BackendStoreHub => _backendHub;
    internal FrontendStoreHub FrontendStoreHub => _frontendHub;
    internal BackendDriverDispatcher GetDispatcher() => _backendDispatcher;

    public GfxResourceManager()
    {
        _frontendHub = new FrontendStoreHub();
        _backendHub = new BackendStoreHub();

        RegisterGlDispatcher();
        _backendDispatcher = new BackendDriverDispatcher(_dispatchers);
    }

    internal void AttachStore(IGraphicsDriver driver)
    {
        _backendHub.AttachStore(driver);
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
        RegisterDispatcher<GlTextureHandle>(ResourceKind.Texture);
        RegisterDispatcher<GlShaderHandle>(ResourceKind.Shader);
        RegisterDispatcher<GlMeshHandle>(ResourceKind.Mesh);
        RegisterDispatcher<GlVboHandle>(ResourceKind.VertexBuffer);
        RegisterDispatcher<GlIboHandle>(ResourceKind.IndexBuffer);
        RegisterDispatcher<GlFboHandle>(ResourceKind.FrameBuffer);
        RegisterDispatcher<GlRboHandle>(ResourceKind.RenderBuffer);
        RegisterDispatcher<GlUboHandle>(ResourceKind.UniformBuffer);
    }


    private void RegisterDispatcher<THandle>(ResourceKind kind)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.ContainsKey(kind))
            throw new ArgumentException($"Dispatcher '{kind}' is already registered.", nameof(kind));

        var value = new ResourceBackendDispatcher<THandle>(OnDeleted);
        _dispatchers.Add(kind, value);
    }
    /*
    public GfxHandle OnCreate<THandle>(ResourceKind kind, THandle handle, GfxHandle? replaceHandle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        
        var store = _backendHub.GetStore<THandle>(kind);
        if (replaceHandle is { IsValid: true } gfxHandle)
        {
            Console.WriteLine($"Recreated Resource {typeof(THandle).Name}  -  {gfxHandle}");
            return store.Replace(in gfxHandle, handle);
        }

        var newGfxHandler = store.Add(handle);
        var fs = _frontendHub.GetStore(kind);
        return newGfxHandler;
    }
    */

    public void OnDeleted(in DeleteCmd cmd)
    {
        var gfxHandle = cmd.Handle;
        Console.WriteLine($"Deleted {cmd.Handle.Kind} - Id: {cmd.IdValue}");
    }

    // TODO remove from here, used atm
    public FrontendResourceStore<TextureId, TextureMeta> TextureStore => _frontendHub.TextureStore;
    public FrontendResourceStore<ShaderId, ShaderMeta> ShaderStore => _frontendHub.ShaderStore;
    public FrontendResourceStore<MeshId, MeshMeta> MeshStore => _frontendHub.MeshStore;
    public FrontendResourceStore<VertexBufferId, VertexBufferMeta> VboStore => _frontendHub.VboStore;
    public FrontendResourceStore<IndexBufferId, IndexBufferMeta> IboStore => _frontendHub.IboStore;
    public FrontendResourceStore<FrameBufferId, FrameBufferMeta> FboStore => _frontendHub.FboStore;
    public FrontendResourceStore<RenderBufferId, RenderBufferMeta> RboStore => _frontendHub.RboStore;
    public FrontendResourceStore<UniformBufferId, UniformBufferMeta> UboStore => _frontendHub.UboStore;
}


/*
    internal void AttachDispatchers(IGraphicsDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver, nameof(driver));
        ArgumentNullException.ThrowIfNull(_backendDispatcher, nameof(_backendDispatcher));
        ArgumentOutOfRangeException.ThrowIfEqual(_dispatchers.Count, 0);

        driver.AttachDispatcher(_backendDispatcher);
    }


    private void RegisterGlDispatcher()
    {
        RegisterDispatcher<GlTextureHandle>(ResourceKind.Texture);
        RegisterDispatcher<GlShaderHandle>(ResourceKind.Shader);
        RegisterDispatcher<GlMeshHandle>(ResourceKind.Mesh);
        RegisterDispatcher<GlVboHandle>(ResourceKind.VertexBuffer);
        RegisterDispatcher<GlIboHandle>(ResourceKind.IndexBuffer);
        RegisterDispatcher<GlFboHandle>(ResourceKind.FrameBuffer);
        RegisterDispatcher<GlRboHandle>(ResourceKind.RenderBuffer);
        RegisterDispatcher<GlUboHandle>(ResourceKind.UniformBuffer);
    }


    private void RegisterDispatcher<THandle>(ResourceKind kind)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.ContainsKey(kind))
            throw new ArgumentException($"Dispatcher '{kind}' is already registered.", nameof(kind));

        var value = new ResourceBackendDispatcher<THandle>(OnCreate, OnDeleted);
        _dispatchers.Add(kind, value);
    }
*/
