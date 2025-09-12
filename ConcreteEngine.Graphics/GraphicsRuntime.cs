using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics;


public sealed class GraphicsRuntime
{
    private readonly IGraphicsDriver _driver;
    
    private readonly GraphicsContext _context;
    
    private readonly GfxResourceAllocator _allocator;
    private readonly GfxResourceDisposer _disposer;
    private readonly GfxResourceManager _resources;
    private readonly GfxResourceRegistry _registry;
    
    private readonly GfxFactoryHub _factoryHub;

    public IGraphicsContext Context => _context;

    public IGfxResourceAllocator Allocator => _allocator;

    public IGfxResourceDisposer Disposer => _disposer;

    public IGfxResourceRegistry Registry => _registry;
    
    public IGfxFactoryHub FactoryHub => _factoryHub;

    public GraphicsRuntime(GL gl, in FrameInfo initialFrameCtx)
    {
        _driver = new GlBackendDriver(gl);

        _resources = new GfxResourceManager();
        _registry = new GfxResourceRegistry(_resources);
        _context = new GraphicsContext(_driver, _registry);

        _allocator = new GfxResourceAllocator(_driver, _resources, _registry);
        _disposer = new GfxResourceDisposer(_resources, _registry, _driver);

        _factoryHub = new GfxFactoryHub(_context, _resources, _allocator, _registry);

    }
}