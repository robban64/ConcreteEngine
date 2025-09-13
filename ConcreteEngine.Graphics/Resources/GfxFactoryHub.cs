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
    //private readonly FboFactory _fboFactory;

    public IMeshFactory MeshFactory => _meshFactory;
    public IPrimitiveMeshes Primitives => _primitives;
    
    
    public GfxFactoryHub(IGraphicsContext gfx, IGfxResourceManager resources, IGfxResourceAllocator allocator,
        GfxResourceRegistry registry)
    {
        _meshFactory = new MeshFactory(gfx, resources, allocator, registry);
        _primitives = new PrimitiveMeshes();
        _primitives.CreatePrimitives(_meshFactory);
    }
}