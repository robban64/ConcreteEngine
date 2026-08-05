using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Utility;

namespace ConcreteEngine.Graphics;

public sealed class GraphicsRuntime : IDisposable
{
    private static bool _isInitialized;
    private static bool _isDisposed;

    private GfxResourceDisposer _disposer = null!;
    public GfxContext Gfx { get; private set; } = null!;

    public GraphicsRuntime() { }

    public GpuDeviceCapabilities Initialize<T>(IGfxStartupConfig<T> config, out OpenGlVersion version) where T : class
    {
        if (_isInitialized) Throwers.InvalidOperation("GFX has already been initialized.");


        if (config is not GlStartupConfig glConfig)
            throw GraphicsException.UnsupportedFeature("Only OpenGL is supported");

        GfxRegistry.CreateStores();
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
        var buffers = new GfxBuffers();
        var shaders = new GfxShaders(_disposer);
        var textures = new GfxTextures(_disposer);
        var meshes = new GfxMeshes(buffers);
        var frameBuffers = new GfxFrameBuffers(_disposer, textures);
        var cmd = new GfxCommands();

        Gfx = new GfxContext
        {
            Disposer = _disposer,
            Buffers = buffers,
            Meshes = meshes,
            Shaders = shaders,
            Textures = textures,
            FrameBuffers = frameBuffers,
            Commands = cmd,
        };
    }

    private GlCapabilities InitializeDriver(GlStartupConfig glConfig)
    {
        var caps = GlBackendDriver.Initialize(glConfig);
        UniformBufferUtils.Init(caps.Capabilities.UniformBufferOffsetAlignment);
        return caps;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame(Size2D outputSize)
    {
        GfxMetrics.FrameMeta = default;
        Gfx.Commands.BeginFrame(outputSize);
    }

    public void EndFrame()
    {
        if (_disposer.PendingCount > 0) 
            _disposer.DrainDisposeQueue();
        
        Gfx.Buffers.EndFrame(out GfxMetrics.FrameBufferMeta);
        Gfx.Commands.EndFrame();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        GfxRegistry.DisposeAllStores();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RunStaticCtor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(GfxMetrics).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(GfxLog).TypeHandle);
    }
}