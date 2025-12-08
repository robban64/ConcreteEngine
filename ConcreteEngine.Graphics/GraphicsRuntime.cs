#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;

#endregion

namespace ConcreteEngine.Graphics;


public sealed class GraphicsRuntime
{
    private static bool _isInitialized = false;

    private IGraphicsDriver _driver = null!;

    private GfxResourceDisposer _disposer = null!;
    private GfxResourceManager _resources = null!;

    private GfxBuffers _buffers = null!;
    private GfxMeshes _meshes = null!;
    private GfxShaders _shaders = null!;
    private GfxTextures _textures = null!;
    private GfxFrameBuffers _frameBuffers = null!;
    private GfxCommands _cmd = null!;

    public GfxContext Gfx { get; private set; } = null!;

    public GraphicsRuntime()
    {
    }


    public void Initialize<T>(IGfxStartupConfig<T> config) where T : class
    {
        InvalidOpThrower.ThrowIf(_isInitialized, "GFX has already been initialized.");

        if (config is not GlStartupConfig glConfig)
            throw GraphicsException.UnsupportedFeature("Only OpenGL is supported");

        _resources = new GfxResourceManager();
        _disposer = new GfxResourceDisposer(_resources);

        InitDriver(glConfig);

        var gfxCtxInternal = new GfxContextInternal(_driver, _resources, _disposer);

        _buffers = new GfxBuffers(gfxCtxInternal);
        _shaders = new GfxShaders(gfxCtxInternal);
        _textures = new GfxTextures(gfxCtxInternal);
        _meshes = new GfxMeshes(gfxCtxInternal, _buffers);
        _frameBuffers = new GfxFrameBuffers(gfxCtxInternal, _textures);
        _cmd = new GfxCommands(gfxCtxInternal);

        Gfx = new GfxContext
        {
            ResourceManager = _resources,
            Disposer = _disposer,
            Buffers = _buffers,
            Meshes = _meshes,
            Shaders = _shaders,
            Textures = _textures,
            FrameBuffers = _frameBuffers,
            Commands = _cmd
        };

        _isInitialized = true;
    }

    private void InitDriver(GlStartupConfig glConfig)
    {
        var backendOps = _resources.BackendStoreHub.StoreBundle;
        var driver = new GlBackendDriver(glConfig, backendOps, _resources.BackendDispatcher);
        driver.Initialize();
        UniformBufferUtils.Init(driver.Capabilities.UniformBufferOffsetAlignment);
        _driver = driver;
    }

    public void BeginFrame(in GfxFrameInfo frameCtx)
    {
        Gfx.Commands.BeginFrame(in frameCtx);
    }

    public void EndFrame(out GfxFrameResult result)
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue(_driver);

        Gfx.Commands.EndFrame(out result);
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
    }
}