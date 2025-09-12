namespace ConcreteEngine.Graphics.Resources;

public interface IGfxFactoryHub
{
    IMeshFactory MeshFactory { get; }
}
internal class GfxFactoryHub : IGfxFactoryHub
{
    private readonly MeshFactory _meshFactory;
    //private readonly FboFactory _fboFactory;

    public IMeshFactory MeshFactory => _meshFactory;
    
    public GfxFactoryHub(IGraphicsContext gfx, IGfxResourceManager resources, IGfxResourceAllocator allocator,
        GfxResourceRegistry registry)
    {
        _meshFactory = new MeshFactory(gfx, resources, allocator, registry);
    }
}