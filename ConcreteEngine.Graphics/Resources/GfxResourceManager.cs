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

public interface IResourceStoreBridge;

public sealed class ResourceStoreBridge<TId, TMeta, THandle> : IResourceStoreBridge
    where TId : unmanaged, IResourceId
    where TMeta : unmanaged, IResourceMeta
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    private readonly ResourceStore<TId, TMeta> _storeResource;
    private readonly DriverResourceStore<THandle> _storeDriver;

    internal ResourceStore<TId, TMeta> ResourceStore => _storeResource;
    internal DriverResourceStore<THandle> DriverStore => _storeDriver;

    internal ResourceStoreBridge(ResourceStore<TId, TMeta> storeResource, DriverResourceStore<THandle> storeDriver)
    {
        _storeResource = storeResource;
        _storeDriver = storeDriver;
    }
}
/*
internal static class ResourceDispatcher<THandle, TId, TMeta>
    where THandle : unmanaged, IResourceHandle
    where TId     : unmanaged, IResourceId
    where TMeta   : unmanaged, IResourceMeta
{
    // GlBackendDriver calls these; the final action happens in registry methods.
    public static void Created(ResourceKindRegistry registry, ResourceKind kind, THandle handle, TMeta meta)
        => registry.DispatchCreated<THandle, TId, TMeta>(kind, handle, meta);

    public static void Deleted(ResourceKindRegistry registry, ResourceKind kind, THandle handle)
        => registry.DispatchDeleted<THandle, TId, TMeta>(kind, handle);
}

internal static class ResourceDispatcher
{
    internal static GfxResourceManager Manager = null!;
    // GlBackendDriver calls these; the final action happens in registry methods.
    public static void Created<TId, TMeta, THandle>(ResourceKind kind, THandle handle,  TMeta meta)
        where THandle : unmanaged, IResourceHandle
        where TId     : unmanaged, IResourceId
        where TMeta   : unmanaged, IResourceMeta
        => Manager.DispatchCreated<TId, TMeta, THandle>(kind, handle, meta);

    public static void Deleted(ResourceKindRegistry registry, ResourceKind kind, THandle handle)
        => registry.DispatchDeleted<THandle, TId, TMeta>(kind, handle);
}*/

internal delegate GfxHandle BackendCreate<THandle, TMeta>( DriverResourceStore<THandle> backendStore, out TMeta meta)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle> where TMeta : unmanaged;

internal delegate void BackendDelete<THandle>(DriverResourceStore<THandle> backendStore, in GfxHandle handle)
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

public interface IResourceDispatchRouter
{
    /*
    GfxHandle Created<TMeta, THandle>(ResourceKind kind, THandle handle, TMeta meta)
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;

    void Deleted<TId, TMeta, THandle>(GfxHandle gfxHandle)
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>;
        */
}

// Instance dispatcher forwarding to the registry's final triggers.
internal sealed class ResourceDispatcher<TId, TMeta, THandle> : IResourceDispatchRouter
    where TId : unmanaged, IResourceId
    where TMeta : unmanaged, IResourceMeta
    where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
{
    public readonly ResourceStore<TId, TMeta>           Frontend;
    public readonly DriverResourceStore<THandle>        Backend;
    public readonly BackendCreate<THandle, TMeta>       Create;
    public readonly BackendDelete<THandle>              Delete;

    internal ResourceDispatcher(ResourceStore<TId, TMeta> frontend, DriverResourceStore<THandle> backend, BackendCreate<THandle, TMeta> create, BackendDelete<THandle> delete)
    {
        Frontend = frontend;
        Backend = backend;
        Create = create;
        Delete = delete;
    }
/*
    public void Created(ResourceKind kind, THandle handle, TMeta meta) =>
        _registry.DispatchCreated<TId, TMeta, THandle>(kind, handle, meta);

    public void Deleted(GfxHandle handle) => _registry.DispatchDeleted<TId, TMeta, THandle>(handle);
    */
}

internal sealed class GfxResourceManager : IGfxResourceManager
{
    private readonly ResourceStoreHub _resourceStores;
    private readonly BackendStoreHub _backendStores;

    private readonly GfxResourceDisposer _disposer;

    internal BackendStoreHub BackendStoreHub => _backendStores;

    private readonly Dictionary<ResourceKind, IResourceDispatchRouter> _storeRegistry = new(8);

    public GfxResourceManager()
    {
        _resourceStores = new ResourceStoreHub();
        _backendStores = new BackendStoreHub();
        _disposer = new GfxResourceDisposer();
    }

    public void Register<TId, TMeta, THandle>(ResourceKind kind, MakeIdDelegate<TId> onMakeId)
        where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        if (_storeRegistry.ContainsKey(kind))
            throw new ArgumentException($"Resource '{kind}' already registered.", nameof(kind));

        var resourceStore = new ResourceStore<TId, TMeta>(kind, 16, onMakeId);
        var driverStore = new DriverResourceStore<THandle>(GraphicsBackend.OpenGL, kind);
        var value = new ResourceStoreBridge<TId, TMeta, THandle>(resourceStore, driverStore);

        _storeRegistry.Add(kind, value);
    }

    internal TId Create<TId, TMeta, THandle>(ResourceKind kind, THandle handle, out TMeta meta)
        where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var bridge = _storeRegistry[kind];
        if (bridge is not ResourceDispatcher<TId, TMeta, THandle> dispatcher)
            throw new ArgumentException($"Invalid generics constraints on store.");


        var gfxHandle = dispatcher.Create(dispatcher.Backend, out  meta);
        return dispatcher.Frontend.Add(in meta, in gfxHandle);
    }

    internal void Delete<TId, TMeta, THandle>(TId id)
        where TId : unmanaged, IResourceId
        where TMeta : unmanaged, IResourceMeta
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(id.IsValid(), true);
        var bridge = _storeRegistry[gfxHandle.Kind];
        if (bridge is not ResourceDispatcher<TId, TMeta, THandle> dispatcher)
            throw new ArgumentException($"Invalid generics constraints on store.");

        dispatcher.Frontend.Remove(gf)

        var gfxHandle = dispatcher.Create(dispatcher.Backend, out  meta);
        return dispatcher.Frontend.Add(in meta, in gfxHandle);
    }

    public void EnqueueRemoval<TId, TMeta>(TId resourceId, bool replace)
        where TId : unmanaged, IResourceId where TMeta : unmanaged, IResourceMeta
    {
        var store = _resourceStores.GetStore<TId, TMeta>();

        _disposer.EnqueueRemoval(resourceId, replace);
        if (replace) return;

        switch (resourceId)
        {
            case TextureId textureId:
                var texHandle = TextureStore.GetHandle(textureId);
                _backendStores.RemoveResource(ResourceKind.Texture, texHandle);
                break;
            /* case ShaderId shaderId:

                 ref readonly var handleShader = ref _resources.ShaderStore.GetHandle(shaderId);
                 _disposeQueue.Enqueue(handleShader);
                 if (!replace) _resources.ShaderStore.Remove(shaderId, out _);
                 break;
             case MeshId meshId:
                 ref readonly var handleMesh = ref _resources.MeshStore.GetHandle(meshId);
                 _disposeQueue.Enqueue(handleMesh);
                 if (!replace) _resources.MeshStore.Remove(meshId, out _);
                 break;
             case VertexBufferId vboId:
                 ref readonly var handleVbo = ref _resources.VboStore.GetHandle(vboId);
                 _disposeQueue.Enqueue(handleVbo);
                 if (!replace) _resources.VboStore.Remove(vboId, out _);
                 break;
             case IndexBufferId iboId:
                 ref readonly var handleIbo = ref _resources.IboStore.GetHandle(iboId);
                 _disposeQueue.Enqueue(handleIbo);
                 if (!replace) _resources.IboStore.Remove(iboId, out _);
                 break;
             case FrameBufferId fboId:
                 ref readonly var fboHandle = ref _resources.FboStore.GetHandle(fboId);
                 _disposeQueue.Enqueue(fboHandle);
                 if (!replace) _resources.FboStore.Remove(fboId, out _);
                 break;
             case RenderBufferId rboId:
                 ref readonly var rboHandle = ref _resources.RboStore.GetHandle(rboId);
                 _disposeQueue.Enqueue(rboHandle);
                 if (!replace) _resources.RboStore.Remove(rboId, out _);
                 break;
                 */
            default:
                throw new GraphicsException($"Unknown resource type {typeof(TId).Name}");
        }
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