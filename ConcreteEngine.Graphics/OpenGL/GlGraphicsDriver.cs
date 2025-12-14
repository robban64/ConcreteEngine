using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBackendDriver : IGraphicsDriver
{
    private readonly GL _gl;

    private readonly GlCapabilities _capabilities;
    private readonly GlDebugger _debugger;
    private readonly GlDisposer _disposer;
    private readonly GlBuffers _buffers;
    private readonly GlTextures _textures;
    private readonly GlMeshes _meshes;
    private readonly GlShaders _shaders;
    private readonly GlStates _states;
    private readonly GlFrameBuffers _frameBuffers;

    public DeviceCapabilities Capabilities => _capabilities.Caps;


    internal GlBackendDriver(GlStartupConfig config, BackendStoreBundle store, ResourceBackendDispatcher dispatcher)
    {
        _gl = config.DriverContext;
        _capabilities = new GlCapabilities();

        var ctx = new GlCtx { Capabilities = _capabilities, Gl = _gl, Store = store, Dispatcher = dispatcher };

        _debugger = new GlDebugger(_gl);
        _disposer = new GlDisposer(ctx);
        _buffers = new GlBuffers(ctx);
        _textures = new GlTextures(ctx);
        _meshes = new GlMeshes(ctx);
        _shaders = new GlShaders(ctx);
        _states = new GlStates(ctx);
        _frameBuffers = new GlFrameBuffers(ctx);
    }

    public IDriverDebugger Debugger => _debugger;
    public GlDisposer Disposer => _disposer;
    public GlBuffers Buffers => _buffers;
    public GlTextures Textures => _textures;
    public GlMeshes Meshes => _meshes;
    public GlShaders Shaders => _shaders;
    public GlStates States => _states;
    public GlFrameBuffers FrameBuffers => _frameBuffers;

    internal void Initialize()
    {
        _capabilities.CreateDeviceCapabilities(_gl);
        Console.WriteLine($"OpenGL version {Capabilities.GlVersion} loaded.");
        Console.WriteLine("--Device Capability--");
        Console.WriteLine(Capabilities.ToString());

        _debugger.EnableGlDebug();

        _gl.Enable(GLEnum.Dither);
        _gl.Enable(GLEnum.Multisample);
        _gl.Enable(EnableCap.TextureCubeMapSeamless);
        _gl.PixelStore(GLEnum.UnpackAlignment, 1);

        _gl.DepthMask(true);

        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        _gl.FrontFace(FrontFaceDirection.Ccw);
    }


    public void PrepareFrame()
    {
    }

    public void EndFrame()
    {
    }
}