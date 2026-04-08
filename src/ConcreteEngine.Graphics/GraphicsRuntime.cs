using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics;

public sealed class GraphicsRuntime : IDisposable
{
    private static bool _isInitialized;

    private GlBackendDriver _driver = null!;

    private GfxResourceDisposer _disposer = null!;
    private GfxResourceManager _resources = null!;

    private GfxCommands _cmd = null!;
    private GfxBuffers _buffers = null!;
    private GfxMeshes _meshes = null!;
    private GfxShaders _shaders = null!;
    private GfxTextures _textures = null!;
    private GfxFrameBuffers _frameBuffers = null!;

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

        var capabilities = InitializeDriver(glConfig);

        InitializeGfx();
        _isInitialized = true;

        caps = capabilities.Capabilities;
        return capabilities.GlVersion;
    }

    private void InitializeGfx()
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

    private GlCapabilities InitializeDriver(GlStartupConfig glConfig)
    {
        var driver = new GlBackendDriver(glConfig, _resources);
        var caps = driver.Initialize();
        _driver = driver;

        UniformBufferUtils.Init(caps.Capabilities.UniformBufferOffsetAlignment);

        return caps;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame(GfxFrameArgs frameCtx)
    {
        _cmd.BeginFrame(frameCtx);
    }

    public void EndFrame()
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue(_driver);
        ref var meta = ref GfxMetrics.FrameMeta;
        _buffers.EndFrame(out  meta.Buffer);
        _cmd.EndFrame(out meta.Frame);
    }

    public void Dispose()
    {
        GlDraw.Instance.Dispose();

        foreach (var kind in EnumCache<GraphicsKind>.Values)
        {
            _resources.GfxStoreHub.GetStore(kind).Dispose();
            _resources.BackendStoreHub.GetStore(kind).Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RunStaticCtor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(GfxMetrics).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GfxLog).TypeHandle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Warmup()
    {
        Configuration.Warmup.WarmupStore(_resources.BackendStoreHub, _resources.GfxStoreHub);
    }
}