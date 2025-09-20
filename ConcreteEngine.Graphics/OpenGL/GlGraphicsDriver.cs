using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBackendDriver : IGraphicsDriver
{
    private readonly GL _gl;
    private readonly GlCtx _ctx;

    private readonly GraphicsConfiguration _configuration;

    private readonly ResourceBackendDispatcher _dispatcher;
    private readonly BackendOpsHub _store;
    
    private readonly GlCapabilities _capabilities;
    private readonly GlDebugger _debugger;
    private readonly GlDisposer _disposer;
    private readonly GlBuffers _buffers;
    private readonly GlTextures _textures;
    private readonly GlMeshes _meshes;
    private readonly GlShaders _shaders;
    private readonly GlStates _states;
    private readonly GlFrameBuffers _frameBuffers;

    public GraphicsConfiguration Configuration => _configuration;
    public DeviceCapabilities Capabilities => _capabilities.Caps;


    internal GlBackendDriver(GlStartupConfig config, BackendOpsHub store, ResourceBackendDispatcher dispatcher)
    {
        _gl = config.DriverContext;
        _store = store;
        _dispatcher = dispatcher;
        _capabilities = new GlCapabilities();
        _configuration = new GraphicsConfiguration();

        _ctx = new GlCtx { Capabilities = _capabilities, Gl = _gl, Store = _store, Dispatcher = _dispatcher };

        _debugger = new GlDebugger(_gl);
        _disposer = new GlDisposer(_ctx);
        _buffers = new GlBuffers(_ctx);
        _textures = new GlTextures(_ctx);
        _meshes = new GlMeshes(_ctx);
        _shaders = new GlShaders(_ctx);
        _states = new GlStates(_ctx);
        _frameBuffers = new GlFrameBuffers(_ctx);
    }
    
    public GlDebugger Debugger => _debugger;
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

    public void ValidateEndFrame()
    {
        _debugger.CheckGlError();
    }

}