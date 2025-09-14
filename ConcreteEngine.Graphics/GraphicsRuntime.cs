using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics;

public interface IGraphicsRuntime : IDisposable
{
    public IGraphicsContext Context { get; }
    public IGfxResourceAllocator Allocator { get; }
    public IGfxResourceDisposer Disposer { get; }
    public IGfxResourceRegistry Registry { get; }
    public IGfxFactoryHub FactoryHub { get; }

    void Initialize<T>(IGfxStartupConfig<T> config) where T : class;

    void BeginFrame(in FrameInfo frameInfo);
    void EndFrame(out GpuFrameStats stats);

    void Shutdown();
}

public sealed class GraphicsRuntime : IGraphicsRuntime
{
    private IGraphicsDriver _driver = null!;

    private GraphicsContext _context = null!;

    private GfxResourceAllocator _allocator = null!;
    private GfxResourceDisposer _disposer = null!;
    private GfxResourceManager _resources = null!;
    private GfxResourceRegistry _registry = null!;

    private GfxFactoryHub _factoryHub = null!;

    public IGraphicsContext Context => _context;

    public IGfxResourceAllocator Allocator => _allocator;

    public IGfxResourceDisposer Disposer => _disposer;

    public IGfxResourceRegistry Registry => _registry;

    public IGfxFactoryHub FactoryHub => _factoryHub;

    private FrameInfo _frameCtx;

    public GraphicsRuntime()
    {
    }

    public void Initialize<T>(IGfxStartupConfig<T> config) where T : class
    {
        if (config is not GlStartupConfig glConfig)
            throw GraphicsException.UnsupportedFeature("Only OpenGL is supported");

        _resources = new GfxResourceManager();
        _registry = new GfxResourceRegistry(_resources);

        var driver = new GlBackendDriver(_resources.BackendStoreHub);
        driver.Initialize(glConfig);
        _driver = driver;

        _context = new GraphicsContext(_driver, _resources, _registry);
        
        _disposer = new GfxResourceDisposer(_resources, _registry, _driver);
        _allocator = new GfxResourceAllocator(_driver, _resources, _registry, _disposer);
        _factoryHub = new GfxFactoryHub(_context, _resources, _allocator, _registry);

        UniformBufferUtils.Init(_context.Capabilities.UniformBufferOffsetAlignment);
    }

    public void Shutdown()
    {
    }

    public void BeginFrame(in FrameInfo frameInfo)
    {
        _frameCtx = frameInfo;
        _context.BeginFrame(frameInfo);
    }

    public void EndFrame(out GpuFrameStats stats)
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue();
        _context.EndFrame(out stats);
        if (_frameCtx.ResizePending)
        {
            RecreateFbo(_frameCtx.OutputSize);
        }
    }

    private void RecreateFbo(in Vector2D<int> outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.X, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Y, 0);

        var fboStore = _resources.FboStore;
        Console.WriteLine($"Recreating {fboStore.Count} FBO");

        foreach (var fboId in fboStore.IdEnumerator)
        {
            fboId.IsValidOrThrow();
            _disposer.EnqueueRemoval(fboId, true);
            var fboLayout = _registry.FboRegistry.Get(fboId);
            var newDescriptor = fboLayout.GetResizeDescriptor(outputSize);
            _allocator.CreateFrameBuffer(in newDescriptor, out var newMeta);
        }
    }

    private void RecreateSingleFbo(FrameBufferId fboId)
    {
        
        
    }

    public void Dispose()
    {
    }
}