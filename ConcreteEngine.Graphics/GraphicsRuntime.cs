#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;

#endregion

namespace ConcreteEngine.Graphics;

public interface IGraphicsRuntime : IDisposable
{
    public GfxContext Gfx { get; }

    void Initialize<T>(IGfxStartupConfig<T> config) where T : class;
    void BeginFrame(in GfxFrameInfo frameCtx);
    void EndFrame(out GfxFrameResult result);

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

    private GfxFrameInfo _frameCtx;

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

    public void BeginFrame(in GfxFrameInfo frameCtx)
    {
        _frameCtx = frameCtx;
        _gfxContext.Commands.BeginFrame(in frameCtx);
    }

    public void EndFrame(out GfxFrameResult result)
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue(_driver);

        _gfxContext.Commands.EndFrame(out result);
    }

    public void RecreateFbo(ReadOnlySpan<(FrameBufferId Id, Size2D Size)> newSizes)
    {
        Console.WriteLine($"Recreating {newSizes.Length} FBO");
        foreach (var (fboId, size) in newSizes)
            _gfxContext.FrameBuffers.RecreateFrameBuffer(fboId, size);
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
    }

    /*
    private void RecreateFbo(Size2D size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size.Width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size.Height, 0);

        var fboStore = _resources.GfxStoreHub.FboStore;
        Console.WriteLine($"Recreating {fboStore.Count} FBO");

        foreach (var fboId in fboStore.IdEnumerator)
            _gfxContext.FrameBuffers.RecreateFrameBuffer(fboId, size);
    }
*/
}