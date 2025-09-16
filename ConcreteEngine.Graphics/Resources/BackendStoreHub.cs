using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class BackendStoreHub
{
    private readonly Dictionary<ResourceKind, IBackendStoreFacade> _stores = new(8);
    private readonly BackendOpsHub _backendOps;

    public BackendStoreHub()
    {
        RegisterBackendStores();
        _backendOps = new BackendOpsHub(this);
    }

    internal void AttachStore(IGraphicsDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver, nameof(driver));
        driver.AttachStore(_backendOps);
    }
    
    public void Register<TId, THandle, TMeta, TDef>(BackendResourceStore<THandle> store)
        where TId : unmanaged, IResourceId
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
        where TMeta : unmanaged, IResourceMeta
        where TDef : IResourceDef<TId, THandle, TMeta>
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
        where TDef : IResourceDef<TId, THandle, TMeta>
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

/*
 
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   internal BackendResourceStore<THandle> GetStore<THandle>(ResourceKind kind)
       where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
   {
       var store = GetStore(kind);
       if (store is BackendResourceStore<THandle> typed) return typed;
       throw new ArgumentException($"Store {kind} is not {typeof(THandle).Name}");
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   internal IBackendResourceStore GetStore(ResourceKind kind)
   {
       var set = _storeCollection.Facade;
       switch (kind)
       {
           case ResourceKind.Texture: return set.TextureStore;
           case ResourceKind.Shader: return set.ShaderStore;
           case ResourceKind.Mesh: return set.MeshStore;
           case ResourceKind.VertexBuffer: return set.VboStore;
           case ResourceKind.IndexBuffer: return set.IboStore;
           case ResourceKind.FrameBuffer: return set.FboStore;
           case ResourceKind.RenderBuffer: return set.RboStore;
           case ResourceKind.UniformBuffer: return set.UboStore;
           case ResourceKind.Invalid:
           default:
               throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid resource kind.");
       }
   }
internal sealed class BackendStoreHub
{
    private readonly OpenGlStoreCollection _storeCollection;

    private readonly Dictionary<ResourceKind, IBackendStoreAdapter> _byKind = new();


    public BackendStoreHub()
    {
        _storeCollection = new OpenGlStoreCollection();
    }

    public void AttachToDriver(IGraphicsDriver driver)
    {
        driver.AttachStore(_storeCollection);
    }

    public GfxHandle Create<THandle>(ResourceKind kind, THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var store = GetStore<THandle>(kind);
        return store.Add(handle);
    }

    public void Remove(in GfxHandle handle, bool replace)
    {
        var store = GetStore(handle.Kind);
        if (replace) return;
        store.Remove(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal BackendResourceStore<THandle> GetStore<THandle>(ResourceKind kind)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var store = GetStore(kind);
        if (store is BackendResourceStore<THandle> typed) return typed;
        throw new ArgumentException($"Store {kind} is not {typeof(THandle).Name}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IBackendResourceStore GetStore(ResourceKind kind)
    {
        var set = _storeCollection.Facade;
        switch (kind)
        {
            case ResourceKind.Texture: return set.TextureStore;
            case ResourceKind.Shader: return set.ShaderStore;
            case ResourceKind.Mesh: return set.MeshStore;
            case ResourceKind.VertexBuffer: return set.VboStore;
            case ResourceKind.IndexBuffer: return set.IboStore;
            case ResourceKind.FrameBuffer: return set.FboStore;
            case ResourceKind.RenderBuffer: return set.RboStore;
            case ResourceKind.UniformBuffer: return set.UboStore;
            case ResourceKind.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid resource kind.");
        }
    }
}
*/
/*
internal interface IBackendStoreFacade
{
    IBackendResourceStore TextureStore { get; }
    IBackendResourceStore ShaderStore { get; }
    IBackendResourceStore MeshStore { get; }
    IBackendResourceStore VboStore { get; }
    IBackendResourceStore IboStore { get; }
    IBackendResourceStore FboStore { get; }
    IBackendResourceStore RboStore { get; }
    IBackendResourceStore UboStore { get; }
}

internal interface IBackendStoreCollection
{
    IBackendStoreFacade Facade { get; }
}

internal sealed class OpenGlStoreCollection : IBackendStoreCollection
{
    private const GraphicsBackend Backend = GraphicsBackend.OpenGL;
    private readonly BackendResourceStore<GlTextureHandle> _textureStore = new(Backend, ResourceKind.Texture);
    private readonly BackendResourceStore<GlShaderHandle> _shaderStore = new(Backend, ResourceKind.Shader);
    private readonly BackendResourceStore<GlMeshHandle> _meshStore = new(Backend, ResourceKind.Mesh);
    private readonly BackendResourceStore<GlVboHandle> _vboStore = new(Backend, ResourceKind.VertexBuffer);
    private readonly BackendResourceStore<GlIboHandle> _iboStore = new(Backend, ResourceKind.IndexBuffer);
    private readonly BackendResourceStore<GlFboHandle> _fboStore = new(Backend, ResourceKind.FrameBuffer);
    private readonly BackendResourceStore<GlRboHandle> _rboStore = new(Backend, ResourceKind.RenderBuffer);
    private readonly BackendResourceStore<GlUboHandle> _uboStore = new(Backend, ResourceKind.UniformBuffer);

    public IBackendReadResourceStore<GlTextureHandle> TextureStore => _textureStore;
    public IBackendReadResourceStore<GlShaderHandle> ShaderStore => _shaderStore;
    public IBackendReadResourceStore<GlMeshHandle> MeshStore => _meshStore;
    public IBackendReadResourceStore<GlVboHandle> VboStore => _vboStore;
    public IBackendReadResourceStore<GlIboHandle> IboStore => _iboStore;
    public IBackendReadResourceStore<GlFboHandle> FboStore => _fboStore;
    public IBackendReadResourceStore<GlRboHandle> RboStore => _rboStore;
    public IBackendReadResourceStore<GlUboHandle> UboStore => _uboStore;

    private readonly StoreFacade _facade;
    public IBackendStoreFacade Facade => _facade;


    internal OpenGlStoreCollection()
    {
        _facade = new StoreFacade(this);
    }

    internal sealed class StoreFacade : IBackendStoreFacade
    {
        private readonly OpenGlStoreCollection _collection;

        internal StoreFacade(OpenGlStoreCollection collection)
        {
            _collection = collection;
        }

        public IBackendResourceStore TextureStore => _collection._textureStore;
        public IBackendResourceStore ShaderStore => _collection._shaderStore;
        public IBackendResourceStore MeshStore => _collection._meshStore;
        public IBackendResourceStore VboStore => _collection._vboStore;
        public IBackendResourceStore IboStore => _collection._iboStore;
        public IBackendResourceStore FboStore => _collection._fboStore;
        public IBackendResourceStore RboStore => _collection._rboStore;
        public IBackendResourceStore UboStore => _collection._uboStore;
    }

}
*/