using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Graphics.Resources;

internal sealed class BackendStoreHub
{
    private readonly OpenGlStoreCollection _storeCollection;
    

    
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