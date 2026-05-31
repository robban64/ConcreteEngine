using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics;

public sealed class GraphicsRuntime : IDisposable
{
    private static bool _isInitialized;
    private static bool _isDisposed;

    private GlBackendDriver _driver = null!;

    private GfxResourceDisposer _disposer = null!;
    private GfxResourceManager _resources = null!;

    private GfxCommands _cmd = null!;
    private GfxBuffers _buffers = null!;
    private GfxMeshes _meshes = null!;
    private GfxShaders _shaders = null!;
    private GfxTextures _textures = null!;
    private GfxFrameBuffers _frameBuffers = null!;
    private GfxDraw _draw = null!;

    public GfxContext Gfx { get; private set; } = null!;

    public GraphicsRuntime()
    {
    }

    public GpuDeviceCapabilities Initialize<T>(IGfxStartupConfig<T> config, out OpenGlVersion version) where T : class
    {
        InvalidOpThrower.ThrowIf(_isInitialized, "GFX has already been initialized.");

        if (config is not GlStartupConfig glConfig)
            throw GraphicsException.UnsupportedFeature("Only OpenGL is supported");

        _resources = new GfxResourceManager();
        _disposer = new GfxResourceDisposer();

        var capabilities = InitializeDriver(glConfig);

        VertexAttributes.Initialize();
        InitializeGfx();
        _isInitialized = true;

        version = capabilities.GlVersion;
        return capabilities.Capabilities;
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
        _draw = new GfxDraw(gfxCtxInternal);

        Gfx = new GfxContext
        {
            Disposer = _disposer,
            Buffers = _buffers,
            Meshes = _meshes,
            Shaders = _shaders,
            Textures = _textures,
            FrameBuffers = _frameBuffers,
            Commands = _cmd,
            Draw = _draw
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
        _draw.BeginFrame();
    }

    public void EndFrame()
    {
        if (_disposer.PendingCount > 0) _disposer.DrainDisposeQueue(_driver);
        ref var meta = ref GfxMetrics.FrameMeta;
        _buffers.EndFrame(out meta.Buffer);
        _draw.EndFrame(out meta.Frame);
        _cmd.EndFrame();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _draw.Dispose();
        _resources.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RunStaticCtor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(GfxMetrics).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GfxLog).TypeHandle);
    }
}