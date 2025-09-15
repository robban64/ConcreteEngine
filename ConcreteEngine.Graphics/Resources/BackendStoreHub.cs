using System.Runtime.CompilerServices;

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

    public GfxHandle Add<THandle>(ResourceKind kind, in THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var store = GetStore<THandle>(kind);
        return store.Add(handle);
    }

    public void Remove(in GfxHandle handle)
    {
        var store = GetStore(handle.Kind);
        store.Remove(handle);
    }

    public GfxHandle Replace<THandle>(GfxHandle gfxHandle, THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var store = GetStore<THandle>(gfxHandle.Kind);
        return store.Replace(gfxHandle, handle);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DriverResourceStore<THandle> GetStore<THandle>(ResourceKind kind)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle>
    {
        var store = GetStore(kind);
        if (store is DriverResourceStore<THandle> typed) return typed;
        throw new ArgumentException($"Store {kind} is not {typeof(THandle).Name}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IDriverResourceStore GetStore(ResourceKind kind)
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
    IDriverResourceStore TextureStore { get; }
    IDriverResourceStore ShaderStore { get; }
    IDriverResourceStore MeshStore { get; }
    IDriverResourceStore VboStore { get; }
    IDriverResourceStore IboStore { get; }
    IDriverResourceStore FboStore { get; }
    IDriverResourceStore RboStore { get; }
    IDriverResourceStore UboStore { get; }
}

internal interface IBackendStoreCollection
{
    IBackendStoreFacade Facade { get; }
}

internal sealed class OpenGlStoreCollection : IBackendStoreCollection
{
    private const GraphicsBackend Backend = GraphicsBackend.OpenGL;
    private readonly DriverResourceStore<GlTextureHandle> _textureStore = new(Backend, ResourceKind.Texture);
    private readonly DriverResourceStore<GlShaderHandle> _shaderStore = new(Backend, ResourceKind.Shader);
    private readonly DriverResourceStore<GlMeshHandle> _meshStore = new(Backend, ResourceKind.Mesh);
    private readonly DriverResourceStore<GlVboHandle> _vboStore = new(Backend, ResourceKind.VertexBuffer);
    private readonly DriverResourceStore<GlIboHandle> _iboStore = new(Backend, ResourceKind.IndexBuffer);
    private readonly DriverResourceStore<GlFboHandle> _fboStore = new(Backend, ResourceKind.FrameBuffer);
    private readonly DriverResourceStore<GlRboHandle> _rboStore = new(Backend, ResourceKind.RenderBuffer);
    private readonly DriverResourceStore<GlUboHandle> _uboStore = new(Backend, ResourceKind.UniformBuffer);

    public IDriverReadResourceStore<GlTextureHandle> TextureStore => _textureStore;
    public IDriverReadResourceStore<GlShaderHandle> ShaderStore => _shaderStore;
    public IDriverReadResourceStore<GlMeshHandle> MeshStore => _meshStore;
    public IDriverReadResourceStore<GlVboHandle> VboStore => _vboStore;
    public IDriverReadResourceStore<GlIboHandle> IboStore => _iboStore;
    public IDriverReadResourceStore<GlFboHandle> FboStore => _fboStore;
    public IDriverReadResourceStore<GlRboHandle> RboStore => _rboStore;
    public IDriverReadResourceStore<GlUboHandle> UboStore => _uboStore;

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

        public IDriverResourceStore TextureStore => _collection._textureStore;
        public IDriverResourceStore ShaderStore => _collection._shaderStore;
        public IDriverResourceStore MeshStore => _collection._meshStore;
        public IDriverResourceStore VboStore => _collection._vboStore;
        public IDriverResourceStore IboStore => _collection._iboStore;
        public IDriverResourceStore FboStore => _collection._fboStore;
        public IDriverResourceStore RboStore => _collection._rboStore;
        public IDriverResourceStore UboStore => _collection._uboStore;
    }
}