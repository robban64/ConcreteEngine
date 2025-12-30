using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics;

public sealed class GraphicsRuntime
{
    private static bool _isInitialized = false;

    private GlBackendDriver _driver = null!;

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


    public OpenGlVersion Initialize<T>(IGfxStartupConfig<T> config, out GpuDeviceCapabilities caps) where T : class
    {
        InvalidOpThrower.ThrowIf(_isInitialized, "GFX has already been initialized.");

        if (config is not GlStartupConfig glConfig)
            throw GraphicsException.UnsupportedFeature("Only OpenGL is supported");

        _resources = new GfxResourceManager();
        _disposer = new GfxResourceDisposer(_resources);

        var capabilities = InitDriver(glConfig);

        InitGfx();
        _isInitialized = true;

        caps = capabilities.Capabilities;
        return capabilities.GlVersion;
    }

    private void InitGfx()
    {
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
    }

    private GlCapabilities InitDriver(GlStartupConfig glConfig)
    {
        var driver = new GlBackendDriver(glConfig, _resources);
        var caps = driver.Initialize();
        _driver = driver;

        UniformBufferUtils.Init(caps.Capabilities.UniformBufferOffsetAlignment);

        return caps;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame(in GfxFrameArgs frameCtx)
    {
        _cmd.BeginFrame(in frameCtx);
    }

    public void EndFrame()
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue(_driver);

        _buffers.EndFrame(out var bufferMeta);
        _cmd.EndFrame(out var frameMeta);

        GfxMetrics.UploadFrameMetric(in bufferMeta, in frameMeta);
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void WarmUp()
    {
        RuntimeHelpers.RunClassConstructor(typeof(GfxMetrics).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GfxLog).TypeHandle);
        Warmup.WarmupStore(_resources.BackendStoreHub, _resources.GfxStoreHub);
    }
}