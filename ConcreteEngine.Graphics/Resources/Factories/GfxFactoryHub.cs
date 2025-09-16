namespace ConcreteEngine.Graphics.Resources;

public interface IGfxFactoryHub
{
    IMeshFactory MeshFactory { get; }
    IPrimitiveMeshes Primitives { get; }
}
internal class GfxFactoryHub : IGfxFactoryHub
{
    private readonly MeshFactory _meshFactory;

    private readonly PrimitiveMeshes _primitives;
    private readonly FboFactory _fboFactory;
    
    internal MeshFactory MeshFactoryInternal => _meshFactory;
    internal PrimitiveMeshes PrimitivesInternal => _primitives;

    public IMeshFactory MeshFactory => _meshFactory;
    public IPrimitiveMeshes Primitives => _primitives;
    
    
    public GfxFactoryHub(IGraphicsContext gfx, GfxResourceManager resources, GfxResourceAllocator allocator,
        GfxResourceRepository repository)
    {
        _meshFactory = new MeshFactory(gfx, resources.FrontendStoreHub, allocator, repository);
        _primitives = new PrimitiveMeshes();
        _primitives.CreatePrimitives(_meshFactory);
    }
}