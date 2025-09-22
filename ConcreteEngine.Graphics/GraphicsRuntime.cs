using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public interface IGraphicsRuntime : IDisposable
{
    public GfxContext Gfx { get; }

    void Initialize<T>(IGfxStartupConfig<T> config) where T : class;
    void BeginFrame(in FrameInfo frameInfo);
    void EndFrame(out GpuFrameStats stats);

    void Shutdown();
}

public sealed class GraphicsRuntime : IGraphicsRuntime
{
    private static bool _isInitialized = false;

    private IGraphicsDriver _driver = null!;

    private GfxResourceDisposer _disposer = null!;
    private GfxResourceManager _resources = null!;
    private GfxResourceRepository _repository = null!;

    private GfxContext _gfxContext = null!;

    private FrameInfo _frameCtx;

    public GraphicsRuntime()
    {
    }

    public GfxContext Gfx => _gfxContext;

    public void Initialize<T>(IGfxStartupConfig<T> config) where T : class
    {
        InvalidOpThrower.ThrowIf(_isInitialized, "GFX has already been initialized.");

        if (config is not GlStartupConfig glConfig)
            throw GraphicsException.UnsupportedFeature("Only OpenGL is supported");

        _resources = new GfxResourceManager();
        _repository = new GfxResourceRepository(_resources);
        _disposer = new GfxResourceDisposer(_resources, _repository);

        var backendOps = _resources.BackendStoreHub.BackendOps;
        var driver = new GlBackendDriver(glConfig, backendOps, _resources.BackendDispatcher);
        driver.Initialize();
        UniformBufferUtils.Init(driver.Capabilities.UniformBufferOffsetAlignment);

        _driver = driver;

        var gfxCtxInternal = new GfxContextInternal(_driver, _repository, _resources.GfxStoreHub);
        var gfxResourceContext = new GfxResourceContext(_resources, _repository, _disposer);
        _gfxContext = new GfxContext(gfxCtxInternal, gfxResourceContext);

        _isInitialized = true;
    }

    public void Shutdown()
    {
    }

    public void BeginFrame(in FrameInfo frameInfo)
    {
        _frameCtx = frameInfo;
        _gfxContext.Commands.BeginFrame(in frameInfo);
    }

    public void EndFrame(out GpuFrameStats stats)
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue(_driver);

        _gfxContext.Commands.EndFrame(out stats);

        if (_frameCtx.ResizePending)
        {
            RecreateFbo(_frameCtx.OutputSize);
        }
    }

    // TODO pending queue, right now the switch happens directly and is used the next frame.
    private void RecreateFbo(in Vector2D<int> outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.X, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Y, 0);

        var fboStore = _resources.GfxStoreHub.FboStore;
        Console.WriteLine($"Recreating {fboStore.Count} FBO");

        foreach (var fboId in fboStore.IdEnumerator)
        {
            fboId.IsValidOrThrow();
            _gfxContext.FrameBuffers.RecreateAutoResizeFrameBuffer(fboId, outputSize);
        }
    }

    public void Dispose()
    {
    }
}