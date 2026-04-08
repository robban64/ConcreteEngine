using ConcreteEngine.Graphics.Configuration;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBackendDriver : IGraphicsDriver
{
    internal static  GL Gl = null!;

    public GlStates States { get; }
    public GlBuffers Buffers { get; }
    public GlTextures Textures { get; }
    public GlMeshes Meshes { get; }
    public GlShaders Shaders { get; }
    public GlFrameBuffers FrameBuffers { get; }
    public GlDebugger Debugger { get; }
    public GlDisposer Disposer { get; }

    public readonly GlCapabilities Capabilities;


    internal GlBackendDriver(GlStartupConfig config, GfxResourceManager resource)
    {
        Gl = config.DriverContext;
        Capabilities = new GlCapabilities();

        var ctx = new GlCtx
        {
            Capabilities = Capabilities,
            Gl = Gl,
            Store = resource.BackendStoreHub,
            Dispatcher = resource.BackendDispatcher
        };

        Debugger = new GlDebugger(Gl);
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
        Capabilities.CreateDeviceCapabilities(Gl);
        Debugger.EnableGlDebug();

        Gl.Enable(GLEnum.Dither);
        Gl.Enable(GLEnum.Multisample);
        Gl.Enable(EnableCap.TextureCubeMapSeamless);
        Gl.PixelStore(GLEnum.UnpackAlignment, 1);

        Gl.DepthMask(true);

        Gl.Enable(EnableCap.CullFace);
        Gl.CullFace(TriangleFace.Back);
        Gl.FrontFace(FrontFaceDirection.Ccw);

        return Capabilities;
    }
}