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

internal delegate GfxHandle BackendCreate<in THandle>(
    ResourceKind kind,
    THandle handle,
    GfxHandle? replaceHandle = null
) where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal delegate void BackendDelete<THandle>(in DeleteCmd cmd)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

internal interface IResourceBackendDispatcher;

internal sealed class ResourceBackendDispatcher<THandle> : IResourceBackendDispatcher
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public BackendCreate<THandle> OnCreated { get; }
    public BackendDelete<THandle> OnDelete { get; }

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

    public GfxHandle OnCreate<THandle>(ResourceKind kind, THandle handle, GfxHandle? replaceHandle = null)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_dispatchers.TryGetValue(kind, out var dispatcher) &&
            dispatcher is ResourceBackendDispatcher<THandle> backendDispatcher)
        {
            return backendDispatcher.OnCreated(kind, handle, replaceHandle);
        }

        throw new ArgumentException($"Unknown resource kind: {kind}");
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
    private readonly ResourceStoreHub _resourceStores;
    private readonly BackendStoreHub _backendHub;

    private readonly BackendDriverDispatcher _backendDispatcher;

    private readonly Dictionary<ResourceKind, IResourceBackendDispatcher> _dispatchers = new(8);

    internal BackendStoreHub BackendStoreHub => _backendHub;
    internal ResourceStoreHub FrontendStoreHub => _resourceStores;
    internal BackendDriverDispatcher GetDispatcher() => _backendDispatcher;

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
        var fs = _resourceStores.GetStore(kind);
        return newGfxHandler;
    }

    public void OnDeleted(in DeleteCmd cmd)
    {
        var gfxHandle = cmd.Handle;
        Console.WriteLine($"Deleted {cmd.Handle.Kind} - Id: {cmd.IdValue}");

        var store = _backendHub.GetStore(gfxHandle.Kind);
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

        var value = new ResourceBackendDispatcher<THandle>(OnCreate, OnDeleted);
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
