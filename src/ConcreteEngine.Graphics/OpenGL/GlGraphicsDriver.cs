using ConcreteEngine.Graphics.Configuration;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBackendDriver : IGraphicsDriver
{
    private readonly GL _gl;

    public readonly GlCapabilities Capabilities;

    public GlDebugger Debugger { get; }
    public GlDisposer Disposer { get; }
    public GlBuffers Buffers { get; }
    public GlTextures Textures { get; }
    public GlMeshes Meshes { get; }
    public GlShaders Shaders { get; }
    public GlStates States { get; }
    public GlFrameBuffers FrameBuffers { get; }


    internal GlBackendDriver(GlStartupConfig config, GfxResourceManager resource)
    {
        _gl = config.DriverContext;
        Capabilities = new GlCapabilities();

        var ctx = new GlCtx
        {
            Capabilities = Capabilities,
            Gl = _gl,
            Store = resource.BackendStoreHub.StoreBundle,
            Dispatcher = resource.BackendDispatcher
        };

        Debugger = new GlDebugger(_gl);
        Disposer = new GlDisposer(ctx);
        Buffers = new GlBuffers(ctx);
        Textures = new GlTextures(ctx);
        Meshes = new GlMeshes(ctx);
        Shaders = new GlShaders(ctx);
        States = new GlStates(ctx);
        FrameBuffers = new GlFrameBuffers(ctx);
    }


    internal GlCapabilities Initialize()
    {
        Capabilities.CreateDeviceCapabilities(_gl);
        Debugger.EnableGlDebug();

        _gl.Enable(GLEnum.Dither);
        _gl.Enable(GLEnum.Multisample);
        _gl.Enable(EnableCap.TextureCubeMapSeamless);
        _gl.PixelStore(GLEnum.UnpackAlignment, 1);

        _gl.DepthMask(true);

        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        _gl.FrontFace(FrontFaceDirection.Ccw);

        return Capabilities;
    }

}