namespace ConcreteEngine.Graphics.Resources;

//TODO
internal sealed class FboFactory
{
    private readonly IGraphicsContext _gfx;
    private readonly IGfxResourceManager _resources;
    private readonly IGfxResourceAllocator _allocator;
    private readonly GfxResourceRepository _repository;

    public FboFactory(IGraphicsContext gfx, IGfxResourceManager resources, IGfxResourceAllocator allocator, GfxResourceRepository repository)
    {
        _gfx = gfx;
        _resources = resources;
        _allocator = allocator;
        _repository = repository;
    }
    
}